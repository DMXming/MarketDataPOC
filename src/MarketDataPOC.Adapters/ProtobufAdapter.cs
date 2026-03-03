using MarketDataPOC.Core.Abstractions;

namespace MarketDataPOC.Adapters
{
    public class ProtobufAdapter : IProtocolAdapter
    {
        public string Serialize<T>(T obj) => throw new System.NotImplementedException("Protobuf adapter not wired. Add Google.Protobuf if needed.");

        public T? Deserialize<T>(string payload) => throw new System.NotImplementedException("Protobuf adapter not wired. Add Google.Protobuf if needed.");
    }
}