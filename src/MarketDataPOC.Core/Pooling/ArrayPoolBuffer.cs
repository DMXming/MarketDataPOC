using System.Buffers;

namespace MarketDataPOC.Core.Pooling
{
  /// <summary>
    /// 可释放的数组池缓冲区
    /// </summary>
    public ref struct ArrayPoolBuffer
    {
        private byte[]? _array;
        private readonly int _length;

        public ArrayPoolBuffer(int minimumLength)
        {
            _array = ArrayPool<byte>.Shared.Rent(minimumLength);
            _length = _array.Length;
        }

        public readonly Span<byte> Span => _array.AsSpan(0, _length);

        public void Dispose()
        {
            if (_array != null)
            {
                ArrayPool<byte>.Shared.Return(_array);
                _array = null;
            }
        }
    }
}