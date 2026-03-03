using MarketDataPOC.Core.Abstractions;

namespace MarketDataPOC.Adapters
{
    public class BinaryAdapter : IProtocolAdapter
    {
        public string Serialize<T>(T obj) => throw new System.NotImplementedException("Binary serialization not implemented in POC.");

        public T? Deserialize<T>(string payload) => throw new System.NotImplementedException("Binary deserialization not implemented in POC.");
    }
}