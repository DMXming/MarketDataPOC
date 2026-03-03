namespace MarketDataPOC.Core.Models
{
    public class Subscription
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Pattern { get; set; } = string.Empty;  // 支持通配符，如 "600***"
        public string CallbackUrl { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    public class SubscriptionResult
    {
        public bool Success { get; set; }
        public string? SubscriptionId { get; set; }
        public string? Error { get; set; }
    }

    /// <summary>
    /// 处理器指标
    /// </summary>
    public class ProcessorMetrics
    {
        public long ProcessedCount { get; set; }
        public long PublishedCount { get; set; }
        public long ErrorCount { get; set; }
        public long ParseErrorCount { get; set; }
        public long InvalidCount { get; set; }
        public double AvgLatencyMicroseconds { get; set; }
        public double P99LatencyMicroseconds { get; set; }
        public int QueueLength { get; set; }
        public double PoolHitRate { get; set; }
        public DateTime LastUpdateTime { get; set; }
    }
}