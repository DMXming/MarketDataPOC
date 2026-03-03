using MarketDataPOC.Core.Abstractions;
using MarketDataPOC.Core.Models;

namespace MarketDataPOC.Adapters
{
    public class BinaryAdapter : IProtocolAdapter
    {
        public ProtocolType ProtocolType => throw new NotImplementedException();

        public bool TryParse(ReadOnlySpan<byte> data, ref ReusableMarketData marketData)
        {
            throw new NotImplementedException();
        }
    }
}