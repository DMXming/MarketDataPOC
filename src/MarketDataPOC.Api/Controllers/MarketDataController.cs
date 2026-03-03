using Microsoft.AspNetCore.Mvc;
using MarketDataPOC.Core.Models;
using MarketDataPOC.Core.Abstractions;
using System.Threading;
using System.Threading.Tasks;

namespace MarketDataPOC.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class MarketDataController : ControllerBase
    {
        private readonly IMarketDataProcessor _processor;
        private readonly IProtocolAdapter _adapter;

        public MarketDataController(IMarketDataProcessor processor, IProtocolAdapter adapter)
        {
            _processor = processor;
            _adapter = adapter;
        }

        [HttpPost]
        public async Task<IActionResult> Post([FromBody] MarketData data, CancellationToken ct)
        {
            if (data is null)
                return BadRequest();

            await _processor.ProcessAsync(data, ct).ConfigureAwait(false);
            return Accepted();
        }

        [HttpGet("serialize")]
        public IActionResult SerializeSample()
        {
            var sample = new MarketData { Symbol = "SAMP", Price = 1.23m };
            var payload = _adapter.Serialize(sample);
            return Ok(payload);
        }
    }
}