using MarketDataPOC.Core.Abstractions;
using MarketDataPOC.Core.Models;
using MarketDataPOC.Core.Pooling;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Text;

namespace MarketDataPOC.Core.Processing
{

    public class EnhancedMarketDataProcessor : MarketDataProcessor
    {
        private readonly MarketDataHandler _handler;
        private readonly ILogger _logger;
        private readonly ISubscriptionManager _subscriptionManager;

        public EnhancedMarketDataProcessor(
            IEnumerable<IProtocolAdapter> adapters,
            ISubscriptionManager subscriptionManager,
            IOptions<ProcessorOptions> options,
            ILogger<EnhancedMarketDataProcessor> logger,
            ILogger<MarketDataHandler> handlerLogger)
            : base(adapters, subscriptionManager, options)
        {
            _logger = logger;
            _subscriptionManager = subscriptionManager;

            // Use handlerLogger for MarketDataHandler
            _handler = new MarketDataHandler(
                handlerLogger,
                Options.Create(new MarketDataHandler.HandlerOptions
                {
                    EnableValidation = true,
                    EnableDeduplication = true,
                    EnableSequenceCheck = true,
                    EnablePriceSpikeDetection = true,
                    MaxPriceDeviation = 0.3 // 30%
                }));

            // 订阅自己的数据流
            var subscription = Subscribe(_handler);
        }

        protected override async Task ProcessSingleMessage(ReusableMarketData marketData)
        {
            var immutableData = marketData.ToImmutable();

            // 先通过 Handler 处理
            var result = await _handler.HandleAsync(immutableData);

            if (result.Ok && result.ProcessedData.HasValue)
            {
                // 处理成功，继续分发
                _subscriptionManager.Publish(result.ProcessedData.Value);
                NotifyObservers(result.ProcessedData.Value);
                _metrics.IncrementProcessed();
            }
            else
            {
                _logger.LogWarning("Message rejected: {Error}", result.ErrorMessage);
                _metrics.IncrementInvalid();
            }

            _marketDataPool.Return(marketData);
        }
    }
}
