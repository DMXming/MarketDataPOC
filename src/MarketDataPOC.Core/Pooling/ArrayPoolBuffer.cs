using System.Buffers;

namespace MarketDataPOC.Core.Pooling
{
    public class ArrayPoolBuffer
    {
        public byte[] Rent(int minimumLength)
        {
            return ArrayPool<byte>.Shared.Rent(minimumLength);
        }

        public void Return(byte[] buffer)
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }
}