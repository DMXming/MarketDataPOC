using System;

namespace MarketDataPOC.Core.Models
{
    public sealed class MarketData
    {
        public string Symbol { get; init; } = string.Empty;
        public decimal Price { get; init; }
        public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;
    }
}