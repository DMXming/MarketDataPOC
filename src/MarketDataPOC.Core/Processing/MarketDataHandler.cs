using MarketDataPOC.Core.Abstractions;
using MarketDataPOC.Core.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MarketDataPOC.Core.Processing
{
    /// <summary>
    /// 行情数据处理器
    /// 负责数据的校验、清洗、指标计算和序列号管理
    /// </summary>
    public class MarketDataHandler : IObserver<MarketData>, IDisposable
    {
        private readonly ILogger<MarketDataHandler> _logger;
        private readonly HandlerOptions _options;
        private readonly ConcurrentDictionary<string, SymbolState> _symbolStates;
        private readonly ConcurrentQueue<MarketData> _deadLetterQueue;
        private readonly MetricsCollector _metrics;
        private readonly List<IDisposable> _subscriptions;
        private readonly Timer _cleanupTimer;
        private readonly Timer _reportTimer;
        private readonly SemaphoreSlim _semaphore;
        private bool _disposed;

        public MarketDataHandler(
            ILogger<MarketDataHandler> logger,
            IOptions<HandlerOptions> options)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _options = options?.Value ?? new HandlerOptions();

            _symbolStates = new ConcurrentDictionary<string, SymbolState>();
            _deadLetterQueue = new ConcurrentQueue<MarketData>();
            _metrics = new MetricsCollector();
            _subscriptions = new List<IDisposable>();
            _semaphore = new SemaphoreSlim(_options.MaxConcurrentHandlers, _options.MaxConcurrentHandlers);

            // 启动清理定时器
            _cleanupTimer = new Timer(CleanupStaleStates, null,
                TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(5));

            // 启动报告定时器
            _reportTimer = new Timer(ReportMetrics, null,
                TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(30));

            _logger.LogInformation("MarketDataHandler initialized with options: {@Options}", _options);
        }

        /// <summary>
        /// 处理器选项
        /// </summary>
        public class HandlerOptions
        {
            public bool EnableValidation { get; set; } = true;
            public bool EnableDeduplication { get; set; } = true;
            public bool EnableSequenceCheck { get; set; } = true;
            public bool EnableMetrics { get; set; } = true;
            public int MaxConcurrentHandlers { get; set; } = Environment.ProcessorCount;
            public int DeadLetterQueueSize { get; set; } = 10000;
            public int StateCleanupMinutes { get; set; } = 60;
            public long MaxSequenceGap { get; set; } = 1000;
            public double MaxPriceDeviation { get; set; } = 0.5; // 50%
            public bool EnablePriceSpikeDetection { get; set; } = true;
        }

        /// <summary>
        /// 单个标的的状态
        /// </summary>
        private class SymbolState
        {
            public string Symbol { get; }
            public long LastSequenceNumber { get; set; }
            public DateTime LastUpdateTime { get; set; }
            public MarketData? LastMarketData { get; set; }
            public double LastPrice { get; set; }
            public long TotalMessages { get; set; }
            public long DuplicateCount { get; set; }
            public long OutOfOrderCount { get; set; }
            public long InvalidCount { get; set; }
            public ConcurrentQueue<long> RecentSequences { get; }
            public object LockObject { get; } = new object();

            public SymbolState(string symbol)
            {
                Symbol = symbol;
                LastUpdateTime = DateTime.UtcNow;
                RecentSequences = new ConcurrentQueue<long>();
                LastSequenceNumber = -1;
            }
        }

        /// <summary>
        /// 指标收集器
        /// </summary>
        private class MetricsCollector
        {
            private long _totalProcessed;
            private long _totalValid;
            private long _totalInvalid;
            private long _totalDuplicates;
            private long _totalOutOfOrder;
            private long _totalPriceSpikes;
            private readonly ConcurrentDictionary<string, long> _errorCounts = new();
            private readonly Stopwatch _uptimeStopwatch = Stopwatch.StartNew();

            public void IncrementProcessed() => Interlocked.Increment(ref _totalProcessed);
            public void IncrementValid() => Interlocked.Increment(ref _totalValid);
            public void IncrementInvalid() => Interlocked.Increment(ref _totalInvalid);
            public void IncrementDuplicates() => Interlocked.Increment(ref _totalDuplicates);
            public void IncrementOutOfOrder() => Interlocked.Increment(ref _totalOutOfOrder);
            public void IncrementPriceSpikes() => Interlocked.Increment(ref _totalPriceSpikes);

            public void RecordError(string errorType)
            {
                _errorCounts.AddOrUpdate(errorType, 1, (_, count) => count + 1);
            }

            public HandlerMetrics GetMetrics()
            {
                return new HandlerMetrics
                {
                    TotalProcessed = _totalProcessed,
                    TotalValid = _totalValid,
                    TotalInvalid = _totalInvalid,
                    TotalDuplicates = _totalDuplicates,
                    TotalOutOfOrder = _totalOutOfOrder,
                    TotalPriceSpikes = _totalPriceSpikes,
                    ErrorCounts = _errorCounts.ToDictionary(kv => kv.Key, kv => kv.Value),
                    UptimeSeconds = _uptimeStopwatch.Elapsed.TotalSeconds
                };
            }
        }

        /// <summary>
        /// 处理器指标
        /// </summary>
        public class HandlerMetrics
        {
            public long TotalProcessed { get; set; }
            public long TotalValid { get; set; }
            public long TotalInvalid { get; set; }
            public long TotalDuplicates { get; set; }
            public long TotalOutOfOrder { get; set; }
            public long TotalPriceSpikes { get; set; }
            public Dictionary<string, long> ErrorCounts { get; set; } = new();
            public double UptimeSeconds { get; set; }
            public int ActiveSymbols { get; set; }
            public int DeadLetterCount { get; set; }
        }

        /// <summary>
        /// 处理单个行情数据
        /// </summary>
        public async Task<ProcessingResult> HandleAsync(MarketData data)
        {
            if (string.IsNullOrEmpty(data.Symbol))
            {
                _metrics.IncrementInvalid();
                _metrics.RecordError("EmptySymbol");
                return ProcessingResult.Failed("Symbol cannot be empty");
            }

            _metrics.IncrementProcessed();

            // 使用信号量控制并发
            await _semaphore.WaitAsync();
            try
            {
                var stopwatch = Stopwatch.StartNew();

                // 获取或创建标的状体
                var state = _symbolStates.GetOrAdd(data.Symbol,
                    symbol => new SymbolState(symbol));

                // 执行处理
                var result = await ProcessDataAsync(data, state);

                stopwatch.Stop();

                if (result.Ok)
                {
                    _metrics.IncrementValid();
                    _logger.LogTrace("Processed {Symbol} in {ElapsedMs}ms",
                        data.Symbol, stopwatch.Elapsed.TotalMilliseconds);
                }
                else
                {
                    _metrics.IncrementInvalid();
                    _deadLetterQueue.Enqueue(data);

                    // 限制死信队列大小
                    while (_deadLetterQueue.Count > _options.DeadLetterQueueSize)
                    {
                        _deadLetterQueue.TryDequeue(out _);
                    }
                }

                return result;
            }
            finally
            {
                _semaphore.Release();
            }
        }

        /// <summary>
        /// 处理数据
        /// </summary>
        private Task<ProcessingResult> ProcessDataAsync(MarketData data, SymbolState state)
        {
            lock (state.LockObject)
            {
                try
                {
                    state.TotalMessages++;
                    state.LastUpdateTime = DateTime.UtcNow;

                    // 1. 基础数据校验
                    if (_options.EnableValidation)
                    {
                        if (!ValidateBasicData(data))
                        {
                            state.InvalidCount++;
                            return Task.FromResult(ProcessingResult.Failed("Basic validation failed"));
                        }
                    }

                    // 2. 去重检查
                    if (_options.EnableDeduplication && IsDuplicate(data, state))
                    {
                        state.DuplicateCount++;
                        _metrics.IncrementDuplicates();
                        return Task.FromResult(ProcessingResult.Failed("Duplicate message"));
                    }

                    // 3. 序列号检查
                    if (_options.EnableSequenceCheck)
                    {
                        var seqResult = CheckSequence(data, state);
                        if (!seqResult.Ok)
                        {
                            state.OutOfOrderCount++;
                            _metrics.IncrementOutOfOrder();

                            if (_options.EnableSequenceCheck && _options.EnableDeduplication)
                            {
                                // 如果启用严格模式，拒绝乱序消息
                                return Task.FromResult(seqResult);
                            }
                        }
                    }

                    // 4. 价格异常检测
                    if (_options.EnablePriceSpikeDetection && HasPriceSpike(data, state))
                    {
                        _metrics.IncrementPriceSpikes();
                        _logger.LogWarning("Price spike detected for {Symbol}: {OldPrice} -> {NewPrice}",
                            data.Symbol, state.LastPrice, data.Price);
                    }

                    // 5. 计算衍生指标
                    var derivedData = CalculateDerivedMetrics(data, state);

                    // 6. 更新状态
                    UpdateState(data, state);

                    return Task.FromResult(ProcessingResult.Success(derivedData));
                }
                catch (Exception ex)
                {
                    _metrics.RecordError(ex.GetType().Name);
                    _logger.LogError(ex, "Error processing data for {Symbol}", data.Symbol);
                    return Task.FromResult(ProcessingResult.Failed($"Processing error: {ex.Message}"));
                }
            }
        }

        /// <summary>
        /// 基础数据校验
        /// </summary>
        private bool ValidateBasicData(MarketData data)
        {
            // 价格校验
            if (data.Price <= 0 || data.Price > 1000000)
            {
                _logger.LogWarning("Invalid price for {Symbol}: {Price}", data.Symbol, data.Price);
                return false;
            }

            // 数量校验
            if (data.Volume < 0 || data.Volume > 1_000_000_000)
            {
                _logger.LogWarning("Invalid volume for {Symbol}: {Volume}", data.Symbol, data.Volume);
                return false;
            }

            // 时间戳校验（不能是未来的时间，不能太旧）
            var now = DateTime.UtcNow;
            if (data.Timestamp > now.AddSeconds(5))
            {
                _logger.LogWarning("Future timestamp for {Symbol}: {Timestamp}", data.Symbol, data.Timestamp);
                return false;
            }

            if (data.Timestamp < now.AddHours(-1))
            {
                _logger.LogWarning("Too old timestamp for {Symbol}: {Timestamp}", data.Symbol, data.Timestamp);
                return false;
            }

            return true;
        }

        /// <summary>
        /// 检查是否重复消息
        /// </summary>
        private bool IsDuplicate(MarketData data, SymbolState state)
        {
            // 基于序列号去重
            if (data.SequenceNumber > 0 && state.RecentSequences.Contains(data.SequenceNumber))
            {
                _logger.LogDebug("Duplicate sequence {Sequence} for {Symbol}",
                    data.SequenceNumber, data.Symbol);
                return true;
            }

            // 基于时间戳和价格去重（如果序列号不可用）
            if (state.LastMarketData.HasValue)
            {
                var last = state.LastMarketData.Value;
                var timeDiff = (data.Timestamp - last.Timestamp).TotalMilliseconds;

                if (Math.Abs(timeDiff) < 1 &&
                    Math.Abs(data.Price - last.Price) < 0.001 &&
                    data.Volume == last.Volume)
                {
                    _logger.LogDebug("Potential duplicate based on content for {Symbol}", data.Symbol);
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 检查序列号连续性
        /// </summary>
        private ProcessingResult CheckSequence(MarketData data, SymbolState state)
        {
            if (data.SequenceNumber <= 0)
                return ProcessingResult.Success(data); // 无序列号，跳过检查

            if (state.LastSequenceNumber < 0)
            {
                // 第一个消息
                return ProcessingResult.Success(data);
            }

            var expectedSeq = state.LastSequenceNumber + 1;
            var gap = data.SequenceNumber - expectedSeq;

            if (gap == 0)
            {
                // 完美顺序
                return ProcessingResult.Success(data);
            }
            else if (gap > 0)
            {
                if (gap <= _options.MaxSequenceGap)
                {
                    // 允许的间隙
                    _logger.LogDebug("Sequence gap of {Gap} for {Symbol}", gap, data.Symbol);
                    return ProcessingResult.Success(data);
                }
                else
                {
                    // 间隙过大
                    _logger.LogWarning("Large sequence gap of {Gap} for {Symbol}", gap, data.Symbol);
                    return ProcessingResult.Failed($"Sequence gap too large: {gap}");
                }
            }
            else
            {
                // 乱序消息
                _logger.LogDebug("Out of order message for {Symbol}: got {Seq}, expected {Expected}",
                    data.Symbol, data.SequenceNumber, expectedSeq);
                return ProcessingResult.Failed($"Out of order: {data.SequenceNumber} vs {expectedSeq}");
            }
        }

        /// <summary>
        /// 检测价格异常波动
        /// </summary>
        private bool HasPriceSpike(MarketData data, SymbolState state)
        {
            if (state.LastPrice <= 0)
                return false;

            var change = Math.Abs((data.Price - state.LastPrice) / state.LastPrice);
            return change > _options.MaxPriceDeviation;
        }

        /// <summary>
        /// 计算衍生指标
        /// </summary>
        private MarketData CalculateDerivedMetrics(MarketData data, SymbolState state)
        {
            // 这里可以添加各种指标计算
            // 例如：涨跌幅、成交额、均价等

            var derivedData = data; // 实际可能返回包含更多字段的派生类

            if (state.LastPrice > 0)
            {
                var changePercent = (data.Price - state.LastPrice) / state.LastPrice * 100;
                _logger.LogTrace("{Symbol} price change: {Change:F2}%", data.Symbol, changePercent);
            }

            return derivedData;
        }

        /// <summary>
        /// 更新标的状体
        /// </summary>
        private void UpdateState(MarketData data, SymbolState state)
        {
            state.LastMarketData = data;
            state.LastPrice = data.Price;
            state.LastUpdateTime = DateTime.UtcNow;

            if (data.SequenceNumber > 0)
            {
                state.LastSequenceNumber = data.SequenceNumber;

                // 维护最近序列号队列
                state.RecentSequences.Enqueue(data.SequenceNumber);
                while (state.RecentSequences.Count > 1000)
                {
                    state.RecentSequences.TryDequeue(out _);
                }
            }
        }

        /// <summary>
        /// 清理过期状体
        /// </summary>
        private void CleanupStaleStates(object? state)
        {
            var cutoffTime = DateTime.UtcNow.AddMinutes(-_options.StateCleanupMinutes);
            var staleSymbols = _symbolStates
                .Where(kv => kv.Value.LastUpdateTime < cutoffTime)
                .Select(kv => kv.Key)
                .ToList();

            foreach (var symbol in staleSymbols)
            {
                if (_symbolStates.TryRemove(symbol, out var removedState))
                {
                    _logger.LogInformation("Cleaned up stale state for {Symbol}, processed {Total} messages",
                        symbol, removedState.TotalMessages);
                }
            }
        }

        /// <summary>
        /// 报告指标
        /// </summary>
        private void ReportMetrics(object? state)
        {
            var metrics = _metrics.GetMetrics();
            metrics.ActiveSymbols = _symbolStates.Count;
            metrics.DeadLetterCount = _deadLetterQueue.Count;

            _logger.LogInformation(
                "Handler metrics - Processed: {TotalProcessed}, Valid: {TotalValid}, " +
                "Invalid: {TotalInvalid}, Duplicates: {TotalDuplicates}, " +
                "OutOfOrder: {TotalOutOfOrder}, Active: {ActiveSymbols}, DeadLetter: {DeadLetterCount}",
                metrics.TotalProcessed, metrics.TotalValid, metrics.TotalInvalid,
                metrics.TotalDuplicates, metrics.TotalOutOfOrder,
                metrics.ActiveSymbols, metrics.DeadLetterCount);
        }

        /// <summary>
        /// 获取死信队列
        /// </summary>
        public IEnumerable<MarketData> GetDeadLetterMessages(int maxCount = 100)
        {
            return _deadLetterQueue.Take(maxCount).ToList();
        }

        /// <summary>
        /// 重放过期消息
        /// </summary>
        public int RetryDeadLetterMessages(Func<MarketData, Task<bool>> retryHandler)
        {
            var retryCount = 0;
            var tempQueue = new Queue<MarketData>();

            while (_deadLetterQueue.TryDequeue(out var data))
            {
                tempQueue.Enqueue(data);
            }

            foreach (var data in tempQueue)
            {
                try
                {
                    if (retryHandler(data).GetAwaiter().GetResult())
                    {
                        retryCount++;
                    }
                    else
                    {
                        _deadLetterQueue.Enqueue(data);
                    }
                }
                catch
                {
                    _deadLetterQueue.Enqueue(data);
                }
            }

            return retryCount;
        }

        /// <summary>
        /// 实现IObserver接口
        /// </summary>
        public void OnNext(MarketData value)
        {
            HandleAsync(value).GetAwaiter().GetResult();
        }

        public void OnError(Exception error)
        {
            _logger.LogError(error, "Error in observable stream");
            _metrics.RecordError("ObservableError");
        }

        public void OnCompleted()
        {
            _logger.LogInformation("Observable stream completed");
        }

        /// <summary>
        /// 添加订阅
        /// </summary>
        public void AddSubscription(IDisposable subscription)
        {
            _subscriptions.Add(subscription);
        }

        /// <summary>
        /// 释放资源
        /// </summary>
        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;

            _cleanupTimer?.Dispose();
            _reportTimer?.Dispose();
            _semaphore?.Dispose();

            foreach (var sub in _subscriptions)
            {
                sub?.Dispose();
            }
            _subscriptions.Clear();

            _logger.LogInformation("MarketDataHandler disposed");
        }
    }

    /// <summary>
    /// 处理结果
    /// </summary>
    public class ProcessingResult
    {
        public bool Ok { get; set; }
        public string? ErrorMessage { get; set; }
        public MarketData? ProcessedData { get; set; }
        public DateTime ProcessedTime { get; set; } = DateTime.UtcNow;

        public static ProcessingResult Success(MarketData data)
        {
            return new ProcessingResult
            {
                Ok = true,
                ProcessedData = data
            };
        }

        public static ProcessingResult Failed(string error)
        {
            return new ProcessingResult
            {
                Ok = false,
                ErrorMessage = error
            };
        }
    }
}