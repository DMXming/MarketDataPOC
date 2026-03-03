using MarketDataPOC.Core.Models;

namespace MarketDataPOC.Core.Abstractions
{
    public interface IProtocolAdapter
    {
        ProtocolType ProtocolType { get; }

        /// <summary>
        /// 尝试解析原始数据
        /// </summary>
        /// <param name="data">原始字节数据</param>
        /// <param name="marketData">解析后的数据（可重用对象）</param>
        /// <returns>是否解析成功</returns>
        bool TryParse(ReadOnlySpan<byte> data, ref ReusableMarketData marketData);
    }
}