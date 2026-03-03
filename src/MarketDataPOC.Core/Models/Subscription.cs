namespace MarketDataPOC.Core.Models
{
    public sealed class Subscription
    {
        public string Topic { get; init; } = string.Empty;
        public string SubscriberId { get; init; } = string.Empty;
    }
}