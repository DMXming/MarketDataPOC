namespace MarketDataPOC.Core.Abstractions
{
    public interface IProtocolAdapter
    {
        string Serialize<T>(T obj);
        T? Deserialize<T>(string payload);
    }
}