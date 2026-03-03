using System;

namespace MarketDataPOC.Core.Models
{
    /// <summary>
    /// 统一的内部行情数据模型
    /// 使用结构体减少堆分配
    /// </summary>
    public readonly struct MarketData
    {
        public MarketData(
            string symbol,
            double price,
            long volume,
            DateTime timestamp,
            ProtocolType protocol,
            long sequenceNumber = 0,
            string exchange = "")
        {
            Symbol = symbol;
            Price = price;
            Volume = volume;
            Timestamp = timestamp;
            Protocol = protocol;
            SequenceNumber = sequenceNumber;
            Exchange = exchange;
        }

        public string Symbol { get; }
        public double Price { get; }
        public long Volume { get; }
        public DateTime Timestamp { get; }
        public ProtocolType Protocol { get; }
        public long SequenceNumber { get; }
        public string Exchange { get; }

        public double Turnover => Price * Volume;

        public override string ToString() =>
            $"[{Timestamp:HH:mm:ss.fff}] {Symbol}: {Price:F2} x {Volume} ({Protocol})";
    }

    /// <summary>
    /// 可重用的市场数据类（用于对象池）
    /// </summary>
    public class ReusableMarketData
    {
        public string Symbol { get; set; } = string.Empty;
        public double Price { get; set; }
        public long Volume { get; set; }
        public DateTime Timestamp { get; set; }
        public ProtocolType Protocol { get; set; }
        public long SequenceNumber { get; set; }
        public string Exchange { get; set; } = string.Empty;

        public void Reset()
        {
            Symbol = string.Empty;
            Price = 0;
            Volume = 0;
            Timestamp = default;
            Protocol = ProtocolType.Unknown;
            SequenceNumber = 0;
            Exchange = string.Empty;
        }

        public MarketData ToImmutable() => new(
            Symbol, Price, Volume, Timestamp, Protocol, SequenceNumber, Exchange
        );

        public void CopyFrom(MarketData data)
        {
            Symbol = data.Symbol;
            Price = data.Price;
            Volume = data.Volume;
            Timestamp = data.Timestamp;
            Protocol = data.Protocol;
            SequenceNumber = data.SequenceNumber;
            Exchange = data.Exchange;
        }
    }
}