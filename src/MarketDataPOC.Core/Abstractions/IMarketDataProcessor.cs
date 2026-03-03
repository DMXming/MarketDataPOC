using System.Threading;
using System.Threading.Tasks;
using MarketDataPOC.Core.Models;

namespace MarketDataPOC.Core.Abstractions
{
    public interface IMarketDataProcessor
    {
        Task ProcessAsync(MarketData data, CancellationToken ct = default);
    }
}