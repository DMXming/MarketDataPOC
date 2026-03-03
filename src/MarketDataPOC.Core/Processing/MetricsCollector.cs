using MarketDataPOC.Core.Models;
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MarketDataPOC.Core.Processing
{
    public class MetricsCollector
    {
        private readonly ConcurrentQueue<double> _latencySamples = new();
        private readonly ConcurrentDictionary<string, long> _errorCounts = new();
        private readonly Timer _timer;
        private long _publishedCount;
        private long _processedCount;
        private long _parseErrors;
        private long _invalidCount;
        private long _batchCount;
        private long _totalBatchSize;
        private long _totalBatchTimeTicks;

        public MetricsCollector(int reportIntervalSeconds = 5)
        {
            _timer = new Timer(ReportMetrics, null, TimeSpan.FromSeconds(reportIntervalSeconds), TimeSpan.FromSeconds(reportIntervalSeconds));
        }

        public void IncrementPublished() => Interlocked.Increment(ref _publishedCount);

        public void IncrementProcessed() => Interlocked.Increment(ref _processedCount);

        public void IncrementParseErrors() => Interlocked.Increment(ref _parseErrors);

        public void IncrementInvalid() => Interlocked.Increment(ref _invalidCount);

        public void IncrementErrors(string errorType)
        {
            _errorCounts.AddOrUpdate(errorType, 1, (_, count) => count + 1);
        }

        public void RecordBatchProcessing(int batchSize, TimeSpan elapsed)
        {
            Interlocked.Increment(ref _batchCount);
            Interlocked.Add(ref _totalBatchSize, batchSize);
            Interlocked.Add(ref _totalBatchTimeTicks, elapsed.Ticks);

            // 记录延迟样本（每个消息的延迟）
            var perMessageLatency = elapsed.TotalMicroseconds / batchSize;
            _latencySamples.Enqueue(perMessageLatency);

            // 保持样本数量可控
            while (_latencySamples.Count > 10000)
            {
                _latencySamples.TryDequeue(out _);
            }
        }

        public ProcessorMetrics GetCurrent()
        {
            var samples = _latencySamples.ToArray();
            Array.Sort(samples);

            return new ProcessorMetrics
            {
                ProcessedCount = _processedCount,
                ErrorCount = _errorCounts.Sum(x => x.Value),
                AvgLatencyMicroseconds = samples.Length > 0 ? samples.Average() : 0,
                P99LatencyMicroseconds = samples.Length > 0 ? samples[(int)(samples.Length * 0.99)] : 0,
                QueueLength = 0, // 由调用者设置
                PoolHitRate = 0.95 // 简化处理
            };
        }

        private void ReportMetrics(object? state)
        {
            var metrics = GetCurrent();
            Console.WriteLine($"[Metrics] Processed: {metrics.ProcessedCount:N0}, " +
                            $"Avg Latency: {metrics.AvgLatencyMicroseconds:F2}μs, " +
                            $"P99: {metrics.P99LatencyMicroseconds:F2}μs, " +
                            $"Errors: {metrics.ErrorCount}");
        }
    }
}