using System;
using System.Threading;
using System.Threading.Tasks;
using MarketDataPOC.Core.Models;

namespace MarketDataPOC.Simulator
{
    public class DataGenerator
    {
        private readonly GeneratorOptions _options;

        public DataGenerator(GeneratorOptions options)
        {
            _options = options;
        }

        public async Task RunAsync(Func<MarketData, Task> onData, CancellationToken ct = default)
        {
            var rand = new Random();
            while (!ct.IsCancellationRequested)
            {
                var data = new MarketData
                {
                    Symbol = "SYM",
                    Price = (decimal)(rand.NextDouble() * 100),
                    Timestamp = DateTimeOffset.UtcNow
                };

                await onData(data).ConfigureAwait(false);

                await Task.Delay(_options.IntervalMs, ct).ConfigureAwait(false);
            }
        }
    }
}