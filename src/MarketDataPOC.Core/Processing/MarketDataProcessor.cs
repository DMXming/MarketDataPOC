using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Channels;
using System.Threading.Tasks;
using Microsoft.Extensions.ObjectPool;
using Microsoft.Extensions.Options;
using MarketDataPOC.Core.Abstractions;
using MarketDataPOC.Core.Models;
using MarketDataPOC.Core.Pooling;

namespace MarketDataPOC.Core.Processing
{
    /// <summary>
    /// 处理器配置选项
    /// </summary>
    public class ProcessorOptions
    {
        public int ChannelCapacity { get; set; } = 100000;
        public int PoolSize { get; set; } = 10000;
        public int BatchSize { get; set; } = 100;
        public bool EnableMetrics { get; set; } = true;
        public int MetricsIntervalSeconds { get; set; } = 5;
    }

    /// <summary>
    /// 市场数据处理器实现
    /// </summary>
    public class MarketDataProcessor : IMarketDataProcessor
    {
        private readonly Channel<ReusableMarketData> _channel;
        private readonly IProtocolAdapter[] _adapters;
        private readonly ISubscriptionManager _subscriptionManager;
        private readonly ObjectPool<ReusableMarketData> _marketDataPool;
        private readonly MetricsCollector _metrics;
        private readonly ProcessorOptions _options;
        private readonly ConcurrentDictionary<ProtocolType, IProtocolAdapter> _adapterMap;
        private readonly Task _processingTask;
        private readonly CancellationTokenSource _cts = new();
        private readonly List<IObserver<MarketData>> _observers = new();

        public MarketDataProcessor(
            IEnumerable<IProtocolAdapter> adapters,
            ISubscriptionManager subscriptionManager,
            IOptions<ProcessorOptions> options)
        {
            _adapters = adapters as IProtocolAdapter[] ?? throw new ArgumentNullException(nameof(adapters));
            _subscriptionManager = subscriptionManager ?? throw new ArgumentNullException(nameof(subscriptionManager));
            _options = options.Value;

            // 初始化适配器映射
            _adapterMap = new ConcurrentDictionary<ProtocolType, IProtocolAdapter>();
            foreach (var adapter in _adapters)
            {
                _adapterMap[adapter.ProtocolType] = adapter;
            }

            // 配置高性能通道
            _channel = Channel.CreateBounded<ReusableMarketData>(
                new BoundedChannelOptions(_options.ChannelCapacity)
                {
                    FullMode = BoundedChannelFullMode.Wait,
                    SingleReader = false,
                    SingleWriter = false,
                    AllowSynchronousContinuations = false
                });

            // 初始化对象池
            _marketDataPool = new DefaultObjectPool<ReusableMarketData>(
                new MarketDataPoolPolicy(),
                _options.PoolSize);

            _metrics = new MetricsCollector(_options.MetricsIntervalSeconds);

            // 启动处理任务
            _processingTask = Task.Run(ProcessMessagesAsync);
        }

        public async ValueTask PublishAsync(ReadOnlyMemory<byte> data, ProtocolType protocol)
        {
            var adapter = GetAdapter(protocol);
            if (adapter == null)
            {
                _metrics.IncrementErrors($"No adapter for protocol: {protocol}");
                return;
            }

            // 从池中获取可重用对象
            var marketData = _marketDataPool.Get();
            marketData.Reset();

            try
            {
                if (adapter.TryParse(data.Span, ref marketData))
                {
                    marketData.Timestamp = DateTime.UtcNow;
                    marketData.Protocol = protocol;

                    // 尝试写入通道
                    if (!_channel.Writer.TryWrite(marketData))
                    {
                        // 通道满，等待异步写入
                        await _channel.Writer.WriteAsync(marketData, _cts.Token);
                    }

                    _metrics.IncrementPublished();
                }
                else
                {
                    // 解析失败，归还对象
                    _marketDataPool.Return(marketData);
                    _metrics.IncrementParseErrors();
                }
            }
            catch (Exception ex)
            {
                // 发生异常，归还对象
                _marketDataPool.Return(marketData);
                _metrics.IncrementErrors(ex.Message);
                throw;
            }
        }

        private IProtocolAdapter? GetAdapter(ProtocolType protocol)
        {
            _adapterMap.TryGetValue(protocol, out var adapter);
            return adapter;
        }

        private async Task ProcessMessagesAsync()
        {
            var reader = _channel.Reader;
            var batch = new List<ReusableMarketData>(_options.BatchSize);
            var stopwatch = new Stopwatch();

            try
            {
                while (await reader.WaitToReadAsync(_cts.Token))
                {
                    stopwatch.Restart();
                    batch.Clear();

                    // 批量读取
                    while (batch.Count < _options.BatchSize && reader.TryRead(out var item))
                    {
                        batch.Add(item);
                    }

                    if (batch.Count == 0) continue;

                    // 并行处理批次
                    Parallel.ForEach(batch, marketData =>
                    {
                        ProcessSingleMessage(marketData);
                    });

                    stopwatch.Stop();
                    _metrics.RecordBatchProcessing(batch.Count, stopwatch.Elapsed);
                }
            }
            catch (OperationCanceledException)
            {
                // 正常取消
            }
            catch (Exception ex)
            {
                _metrics.IncrementErrors($"Processing error: {ex.Message}");
            }
        }

        private void ProcessSingleMessage(ReusableMarketData marketData)
        {
            try
            {
                // 数据校验
                if (!ValidateMarketData(marketData))
                {
                    _metrics.IncrementInvalid();
                    _marketDataPool.Return(marketData);
                    return;
                }

                // 转换为不可变对象用于分发
                var immutableData = marketData.ToImmutable();

                // 分发给订阅者
                _subscriptionManager.Publish(immutableData);

                // 通知观察者
                NotifyObservers(immutableData);

                _metrics.IncrementProcessed();

                // 归还对象到池
                _marketDataPool.Return(marketData);
            }
            catch (Exception ex)
            {
                _metrics.IncrementErrors($"Process error: {ex.Message}");
                _marketDataPool.Return(marketData);
            }
        }

        private bool ValidateMarketData(ReusableMarketData data)
        {
            if (string.IsNullOrEmpty(data.Symbol))
                return false;

            if (data.Price <= 0 || data.Price > 1000000)
                return false;

            if (data.Volume < 0 || data.Volume > 1000000000)
                return false;

            if (data.Timestamp > DateTime.UtcNow.AddSeconds(5))
                return false;

            return true;
        }

        private void NotifyObservers(MarketData data)
        {
            foreach (var observer in _observers)
            {
                try
                {
                    observer.OnNext(data);
                }
                catch
                {
                    // 忽略单个观察者的异常
                }
            }
        }

        public ProcessorMetrics GetMetrics()
        {
            var metrics = _metrics.GetCurrent();
            metrics.QueueLength = _channel.Reader.Count;
            return metrics;
        }

        public IDisposable Subscribe(IObserver<MarketData> observer)
        {
            _observers.Add(observer);
            return new Unsubscriber(_observers, observer);
        }

        private class Unsubscriber : IDisposable
        {
            private readonly List<IObserver<MarketData>> _observers;
            private readonly IObserver<MarketData> _observer;

            public Unsubscriber(List<IObserver<MarketData>> observers, IObserver<MarketData> observer)
            {
                _observers = observers;
                _observer = observer;
            }

            public void Dispose()
            {
                _observers.Remove(_observer);
            }
        }
    }
}