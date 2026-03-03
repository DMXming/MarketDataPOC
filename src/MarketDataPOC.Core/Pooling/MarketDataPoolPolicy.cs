using Microsoft.Extensions.ObjectPool;
using MarketDataPOC.Core.Models;

namespace MarketDataPOC.Core.Pooling
{
    public class MarketDataPoolPolicy : PooledObjectPolicy<ReusableMarketData>
    {
        public override ReusableMarketData Create()
        {
            return new ReusableMarketData();
        }

        public override bool Return(ReusableMarketData obj)
        {
            obj.Reset();
            return true;
        }
    }
}