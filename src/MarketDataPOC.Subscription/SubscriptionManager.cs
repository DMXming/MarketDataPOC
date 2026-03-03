using System.Collections.Concurrent;
using MarketDataPOC.Core.Models;

namespace MarketDataPOC.Subscription
{
    public class SubscriptionManager
    {
        private readonly ConcurrentDictionary<string, ConcurrentBag<string>> _topics = new();

        public void Subscribe(string topic, string subscriberId)
        {
            var bag = _topics.GetOrAdd(topic, _ => new ConcurrentBag<string>());
            bag.Add(subscriberId);
        }

        public void Unsubscribe(string topic, string subscriberId)
        {
            // Simple POC: no removal from ConcurrentBag - production code would use a thread-safe list or other structure
        }
    }
}