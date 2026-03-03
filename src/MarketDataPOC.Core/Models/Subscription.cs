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
}