using MarketDataPOC.Core.Abstractions;

// 简化的Protobuf消息定义 (实际应该从.proto文件生成)
namespace MarketDataPOC.Adapters.Protobuf
{
    [Serializable]
    public class MarketDataProto
    {
        public string Symbol { get; set; } = string.Empty;
        public double Price { get; set; }
        public long Volume { get; set; }
        public long SequenceNumber { get; set; }
        public string Exchange { get; set; } = string.Empty;
    }
}

namespace MarketDataPOC.Adapters
{
    using MarketDataPOC.Adapters.Protobuf;
    using MarketDataPOC.Core.Models;

    public class ProtobufAdapter : IProtocolAdapter
    {
        public ProtocolType ProtocolType => ProtocolType.Protobuf;

        public bool TryParse(ReadOnlySpan<byte> data, ref ReusableMarketData marketData)
        {
            try
            {
                // 简化的Protobuf解析
                // 实际应使用生成的解析代码

                // 模拟解析过程
                // 这里为了演示，假设Protobuf数据前4字节是长度，后面是UTF8编码的JSON
                if (data.Length < 4)
                    return false;

                // 伪造解析 - 实际应使用protobuf库
                // 这里简单处理，假设数据是JSON格式
                var jsonData = System.Text.Encoding.UTF8.GetString(data.Slice(4));

                // 使用JSON解析器
                var jsonAdapter = new JsonAdapter();
                return jsonAdapter.TryParse(System.Text.Encoding.UTF8.GetBytes(jsonData), ref marketData);
            }
            catch
            {
                return false;
            }
        }
    }
}