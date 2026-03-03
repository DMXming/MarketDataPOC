using MarketDataPOC.Core.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace MarketDataPOC.Core.Abstractions
{
    public interface ISubscriptionManager
    {
        SubscriptionResult Subscribe(Subscription subscription);
        bool Unsubscribe(string subscriptionId);
        IEnumerable<Subscription> GetSubscribers(string symbol);
        void Publish(MarketData data);
    }
}
