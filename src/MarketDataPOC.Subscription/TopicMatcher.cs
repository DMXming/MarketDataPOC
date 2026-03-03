namespace MarketDataPOC.Subscriptions
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.RegularExpressions;

    /// <summary>
    /// 主题匹配器，支持通配符模式
    /// 模式格式:
    /// * - 匹配任意字符序列
    /// ? - 匹配单个字符
    /// 600*** - 匹配以600开头的6位代码
    /// </summary>
    public class TopicMatcher
    {
        private readonly ConcurrentDictionary<string, PatternEntry> _patterns = new();

        private class PatternEntry
        {
            public string Pattern { get; }
            public Regex Regex { get; }
            public HashSet<string> Subscribers { get; } = new();

            public PatternEntry(string pattern)
            {
                Pattern = pattern;
                Regex = ConvertToRegex(pattern);
            }
        }

        public void AddPattern(string pattern, string subscriberId)
        {
            var entry = _patterns.GetOrAdd(pattern, p => new PatternEntry(p));
            lock (entry)
            {
                entry.Subscribers.Add(subscriberId);
            }
        }

        public void RemovePattern(string pattern, string subscriberId)
        {
            if (_patterns.TryGetValue(pattern, out var entry))
            {
                lock (entry)
                {
                    entry.Subscribers.Remove(subscriberId);
                    if (entry.Subscribers.Count == 0)
                    {
                        _patterns.TryRemove(pattern, out _);
                    }
                }
            }
        }

        public IEnumerable<string> Match(string topic)
        {
            var results = new HashSet<string>();

            foreach (var entry in _patterns.Values)
            {
                if (entry.Regex.IsMatch(topic))
                {
                    lock (entry)
                    {
                        foreach (var subscriber in entry.Subscribers)
                        {
                            results.Add(subscriber);
                        }
                    }
                }
            }

            return results;
        }

        private static Regex ConvertToRegex(string pattern)
        {
            // 将通配符模式转换为正则表达式
            string regexPattern = "^" + Regex.Escape(pattern)
                .Replace("\\*", ".*")
                .Replace("\\?", ".") + "$";

            return new Regex(regexPattern, RegexOptions.Compiled | RegexOptions.IgnoreCase);
        }
    }
}