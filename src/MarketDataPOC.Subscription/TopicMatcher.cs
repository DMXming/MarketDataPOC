namespace MarketDataPOC.Subscription
{
    public static class TopicMatcher
    {
        public static bool Matches(string topicPattern, string topic)
        {
            // Very small pattern matcher for POC (supports '*' suffix)
            if (topicPattern.EndsWith("*"))
            {
                var prefix = topicPattern.TrimEnd('*');
                return topic.StartsWith(prefix);
            }

            return string.Equals(topicPattern, topic, System.StringComparison.OrdinalIgnoreCase);
        }
    }
}