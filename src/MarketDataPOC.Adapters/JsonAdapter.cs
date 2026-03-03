using System;
using System.Buffers;
using System.Buffers.Text;
using System.Text;
using System.Text.Json;
using MarketDataPOC.Core.Abstractions;
using MarketDataPOC.Core.Models;

namespace MarketDataPOC.Adapters
{
    public class JsonAdapter : IProtocolAdapter
    {
        private static readonly JsonSerializerOptions _jsonOptions = new()
        {
            PropertyNameCaseInsensitive = true,
            AllowTrailingCommas = true
        };

        public ProtocolType ProtocolType => ProtocolType.Json;

        public bool TryParse(ReadOnlySpan<byte> data, ref ReusableMarketData marketData)
        {
            try
            {
                // 解析JSON
                var reader = new Utf8JsonReader(data);

                string? symbol = null;
                double price = 0;
                long volume = 0;
                long seqNum = 0;
                string? exchange = null;

                while (reader.Read())
                {
                    if (reader.TokenType == JsonTokenType.PropertyName)
                    {
                        var propertyName = reader.GetString();
                        reader.Read();

                        switch (propertyName?.ToLowerInvariant())
                        {
                            case "symbol":
                            case "s":
                                symbol = reader.GetString();
                                break;
                            case "price":
                            case "p":
                                price = reader.GetDouble();
                                break;
                            case "volume":
                            case "v":
                                volume = reader.GetInt64();
                                break;
                            case "seq":
                            case "sequence":
                                seqNum = reader.GetInt64();
                                break;
                            case "exchange":
                            case "ex":
                                exchange = reader.GetString();
                                break;
                        }
                    }
                }

                if (string.IsNullOrEmpty(symbol) || price <= 0)
                {
                    return false;
                }

                // 填充可重用对象
                marketData.Symbol = symbol;
                marketData.Price = price;
                marketData.Volume = volume;
                marketData.SequenceNumber = seqNum;
                marketData.Exchange = exchange ?? string.Empty;

                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}