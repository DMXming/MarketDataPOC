using System.Text.Json;
using MarketDataPOC.Core.Abstractions;

namespace MarketDataPOC.Adapters
{
    public class JsonAdapter : IProtocolAdapter
    {
        private readonly JsonSerializerOptions _options = new() { PropertyNameCaseInsensitive = true };

        public string Serialize<T>(T obj) => JsonSerializer.Serialize(obj, _options);

        public T? Deserialize<T>(string payload) => JsonSerializer.Deserialize<T>(payload, _options);
    }
}