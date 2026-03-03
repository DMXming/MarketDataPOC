using MarketDataPOC.Core.Abstractions;
using MarketDataPOC.Core.Models;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace MarketDataPOC.Subscriptions
{
    public class SubscriptionManager : ISubscriptionManager
    {
        private readonly ConcurrentDictionary<string, Subscription> _subscriptions = new();
        private readonly ConcurrentDictionary<string, HashSet<string>> _symbolSubscriptions = new();
        private readonly TopicMatcher _topicMatcher;
        private readonly ILogger<SubscriptionManager> _logger;
        private readonly ReaderWriterLockSlim _rwLock = new();

        public SubscriptionManager(ILogger<SubscriptionManager> logger)
        {
            _logger = logger;
            _topicMatcher = new TopicMatcher();
        }

        public SubscriptionResult Subscribe(Subscription subscription)
        {
            try
            {
                if (string.IsNullOrEmpty(subscription.Pattern))
                {
                    return new SubscriptionResult { Success = false, Error = "Pattern cannot be empty" };
                }

                _rwLock.EnterWriteLock();
                try
                {
                    // 存储订阅
                    if (!_subscriptions.TryAdd(subscription.Id, subscription))
                    {
                        return new SubscriptionResult { Success = false, Error = "Subscription ID already exists" };
                    }

                    // 解析模式并建立索引
                    if (subscription.Pattern == "*")
                    {
                        // 通配符订阅，不需要具体索引
                    }
                    else if (subscription.Pattern.Contains('*') || subscription.Pattern.Contains('?'))
                    {
                        // 通配符模式，存入匹配器
                        _topicMatcher.AddPattern(subscription.Pattern, subscription.Id);
                    }
                    else
                    {
                        // 精确匹配
                        var subscribers = _symbolSubscriptions.GetOrAdd(subscription.Pattern, _ => new HashSet<string>());
                        subscribers.Add(subscription.Id);
                    }

                    _logger.LogInformation("Subscription added: {SubscriptionId} for pattern {Pattern}",
                        subscription.Id, subscription.Pattern);

                    return new SubscriptionResult
                    {
                        Success = true,
                        SubscriptionId = subscription.Id
                    };
                }
                finally
                {
                    _rwLock.ExitWriteLock();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to add subscription");
                return new SubscriptionResult { Success = false, Error = ex.Message };
            }
        }

        public bool Unsubscribe(string subscriptionId)
        {
            _rwLock.EnterWriteLock();
            try
            {
                if (!_subscriptions.TryRemove(subscriptionId, out var subscription))
                {
                    return false;
                }

                // 从索引中移除
                if (subscription.Pattern.Contains('*') || subscription.Pattern.Contains('?'))
                {
                    _topicMatcher.RemovePattern(subscription.Pattern, subscriptionId);
                }
                else
                {
                    if (_symbolSubscriptions.TryGetValue(subscription.Pattern, out var subscribers))
                    {
                        subscribers.Remove(subscriptionId);
                        if (subscribers.Count == 0)
                        {
                            _symbolSubscriptions.TryRemove(subscription.Pattern, out _);
                        }
                    }
                }

                _logger.LogInformation("Subscription removed: {SubscriptionId}", subscriptionId);
                return true;
            }
            finally
            {
                _rwLock.ExitWriteLock();
            }
        }

        public IEnumerable<Subscription> GetSubscribers(string symbol)
        {
            var result = new HashSet<Subscription>();

            _rwLock.EnterReadLock();
            try
            {
                // 精确匹配
                if (_symbolSubscriptions.TryGetValue(symbol, out var exactMatches))
                {
                    foreach (var id in exactMatches)
                    {
                        if (_subscriptions.TryGetValue(id, out var sub))
                        {
                            result.Add(sub);
                        }
                    }
                }

                // 通配符匹配
                var patternMatches = _topicMatcher.Match(symbol);
                foreach (var id in patternMatches)
                {
                    if (_subscriptions.TryGetValue(id, out var sub))
                    {
                        result.Add(sub);
                    }
                }

                // 全局通配符
                if (_symbolSubscriptions.TryGetValue("*", out var globalMatches))
                {
                    foreach (var id in globalMatches)
                    {
                        if (_subscriptions.TryGetValue(id, out var sub))
                        {
                            result.Add(sub);
                        }
                    }
                }
            }
            finally
            {
                _rwLock.ExitReadLock();
            }

            return result;
        }

        public void Publish(MarketData data)
        {
            var subscribers = GetSubscribers(data.Symbol);

            foreach (var subscriber in subscribers)
            {
                // 实际应该异步调用回调URL
                // 这里简化处理，只记录日志
                _logger.LogDebug("Publishing {Symbol} to {SubscriptionId}", data.Symbol, subscriber.Id);
            }
        }
    }
}