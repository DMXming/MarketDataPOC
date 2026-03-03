using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Diagnosers;
using MarketDataPOC.Core.Abstractions;
using MarketDataPOC.Core.Models;
using MarketDataPOC.Core.Processing;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace MarketDataPOC.Tests.Benchmarks
{
    [MemoryDiagnoser]
    public class ProcessorBenchmarks
    {
        private EnhancedMarketDataProcessor _processor;
        private IProtocolAdapter _adapter;
        private ISubscriptionManager _subscriptionManager;
        private byte[] _payload;

        [GlobalSetup]
        public void Setup()
        {
            _adapter = new BenchmarkAdapter();
            _subscriptionManager = new NoopSubscriptionManager();
            _processor = new EnhancedMarketDataProcessor(
                new[] { _adapter },
                _subscriptionManager,
                Options.Create(new ProcessorOptions()),
                NullLogger<EnhancedMarketDataProcessor>.Instance,
                NullLogger<MarketDataHandler>.Instance);

            _payload = new byte[] { 0x1, 0x2, 0x3 };
        }

        [Benchmark]
        public async Task PublishAsync_Benchmark()
        {
            await _processor.PublishAsync(_payload, ProtocolType.Json);
            await Task.Yield();
        }

        private class BenchmarkAdapter : IProtocolAdapter
        {
            public ProtocolType ProtocolType => ProtocolType.Json;

            public bool TryParse(ReadOnlySpan<byte> data, ref ReusableMarketData marketData)
            {
                marketData.Symbol = "BM";
                marketData.Price = 1;
                marketData.Volume = 1;
                marketData.SequenceNumber = 1;
                marketData.Timestamp = System.DateTime.UtcNow;
                return true;
            }
        }

        private class NoopSubscriptionManager : ISubscriptionManager
        {
            public IEnumerable<Subscription> GetSubscribers(string symbol)
            {
                throw new NotImplementedException();
            }

            public void Publish(MarketData data) { }

            public SubscriptionResult Subscribe(Subscription subscription)
            {
                throw new NotImplementedException();
            }

            public bool Unsubscribe(string subscriptionId)
            {
                throw new NotImplementedException();
            }
        }
    }
}