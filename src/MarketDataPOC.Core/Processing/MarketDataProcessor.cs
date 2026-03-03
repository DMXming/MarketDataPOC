using System.Threading;
using System.Threading.Tasks;
using MarketDataPOC.Core.Abstractions;
using MarketDataPOC.Core.Models;
using MarketDataPOC.Core.Pooling;

namespace MarketDataPOC.Core.Processing
{
    public class MarketDataProcessor : IMarketDataProcessor
    {
        private readonly MarketDataPool _pool;

        public MarketDataProcessor(MarketDataPool pool)
        {
            _pool = pool;
        }

        public Task ProcessAsync(MarketData data, CancellationToken ct = default)
        {
            // Placeholder processing pipeline:
            // - Rent buffers if needed
            // - Dispatch to handlers / subscribers
            // - Return buffers

            var buffer = _pool.Rent();
            try
            {
                // real processing would use the buffer and pipeline here
            }
            finally
            {
                _pool.Return(buffer);
            }

            return Task.CompletedTask;
        }
    }
}