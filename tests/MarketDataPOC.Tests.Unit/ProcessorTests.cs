using System;
using System.Diagnostics;
using System.Threading.Tasks;
using MarketDataPOC.Core.Abstractions;
using MarketDataPOC.Core.Models;
using MarketDataPOC.Core.Processing;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace MarketDataPOC.Tests.Unit
{
    public class ProcessorTests
    {
        [Fact]
        public async Task PublishAsync_WhenAdapterParses_PublishesProcessedMarketData()
        {
            var adapter = new FakeAdapter();
            var subscriptionManager = new FakeSubscriptionManager();
            var options = Options.Create(new ProcessorOptions());
            var processor = new EnhancedMarketDataProcessor(
                new[] { adapter },
                subscriptionManager,
                options,
                NullLogger<EnhancedMarketDataProcessor>.Instance,
                NullLogger<MarketDataHandler>.Instance);

            var payload = new byte[] { 0x01, 0x02, 0x03 };

            await processor.PublishAsync(payload, ProtocolType.Json);

            var sw = Stopwatch.StartNew();
            while (sw.Elapsed < TimeSpan.FromSeconds(2) && subscriptionManager.LastPublished == null)
            {
                await Task.Delay(10);
            }

            Assert.NotNull(subscriptionManager.LastPublished);
            Assert.Equal("TST", subscriptionManager.LastPublished.Value.Symbol);
            Assert.Equal(100d, subscriptionManager.LastPublished.Value.Price);
            Assert.Equal(1L, subscriptionManager.LastPublished.Value.SequenceNumber);
        }

        [Fact]
        public void Subscribe_Unsubscribe_ObserverLifecycle()
        {
            var adapter = new FakeAdapter();
            var subscriptionManager = new FakeSubscriptionManager();
            var options = Options.Create(new ProcessorOptions());
            var processor = new EnhancedMarketDataProcessor(
                new[] { adapter },
                subscriptionManager,
                options,
                NullLogger<EnhancedMarketDataProcessor>.Instance,
                NullLogger<MarketDataHandler>.Instance);

            var observer = new TestObserver();
            var subscription = processor.Subscribe(observer);

            Assert.NotNull(subscription);

            subscription.Dispose();
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

        private class FakeSubscriptionManager : ISubscriptionManager
        {
            public MarketData? LastPublished;

            public IEnumerable<Subscription> GetSubscribers(string symbol)
            {
                throw new NotImplementedException();
            }

            public void Publish(MarketData data)
            {
                LastPublished = data;
            }

            public SubscriptionResult Subscribe(Subscription subscription)
            {
                throw new NotImplementedException();
            }

            public bool Unsubscribe(string subscriptionId)
            {
                throw new NotImplementedException();
            }
        }

        private class TestObserver : IObserver<MarketData>
        {
            public void OnCompleted() { }
            public void OnError(Exception error) { }
            public void OnNext(MarketData value) { }
        }
    }
}