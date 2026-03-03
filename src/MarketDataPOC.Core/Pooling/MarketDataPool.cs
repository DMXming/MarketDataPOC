using System.Collections.Concurrent;

namespace MarketDataPOC.Core.Pooling
{
    // Simple object pool for demonstration
    public class MarketDataPool
    {
        private readonly ConcurrentBag<byte[]> _bag = new();
        private const int BufferSize = 1024;

        public byte[] Rent()
        {
            if (_bag.TryTake(out var buf))
                return buf;
            return new byte[BufferSize];
        }

        public void Return(byte[] buffer)
        {
            if (buffer is not null && buffer.Length == BufferSize)
            {
                _bag.Add(buffer);
            }
        }
    }
}