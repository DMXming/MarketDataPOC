using System.Threading;
using System.Threading.Tasks;
using MarketDataPOC.Core.Models;

namespace MarketDataPOC.Core.Processing
{
    public class MarketDataHandler
    {
        public Task HandleAsync(MarketData data, CancellationToken ct = default)
        {
            // Implement handler logic here (validation, enrichment, routing).
            return Task.CompletedTask;
        }
    }
}