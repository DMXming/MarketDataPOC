using System;
using MarketDataPOC.Core.Abstractions;
using MarketDataPOC.Core.Models;
using Xunit;

namespace MarketDataPOC.Tests.Unit
{
    public class AdapterTests
    {
        [Fact]
        public void FakeAdapter_TryParse_PopulatesMarketData()
        {
            var adapter = new FakeAdapter();
            var data = new byte[] { 0x0 };
            var md = new ReusableMarketData();
            var result = adapter.TryParse(data, ref md);
            Assert.True(result);
            Assert.Equal("TST", md.Symbol);
            Assert.Equal(100d, md.Price);
            Assert.Equal(1L, md.SequenceNumber);
        }

        private class FakeAdapter : IProtocolAdapter
        {
            public ProtocolType ProtocolType => ProtocolType.Json;

            public bool TryParse(ReadOnlySpan<byte> data, ref ReusableMarketData marketData)
            {
                marketData.Symbol = "TST";
                marketData.Price = 100;
                marketData.Volume = 1;
                marketData.SequenceNumber = 1;
                marketData.Timestamp = DateTime.UtcNow;
                return true;
            }
        }
    }
}