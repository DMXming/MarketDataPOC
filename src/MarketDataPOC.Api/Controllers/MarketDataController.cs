using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using MarketDataPOC.Core.Abstractions;
using MarketDataPOC.Core.Models;
using MarketDataPOC.Core.Processing;

namespace MarketDataPOC.Api.Controllers
{
    /// <summary>
    /// 行情数据控制器
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class MarketDataController : ControllerBase
    {
        private readonly IMarketDataProcessor _processor;
        private readonly ISubscriptionManager _subscriptionManager;
        private readonly ILogger<MarketDataController> _logger;

        public MarketDataController(
            IMarketDataProcessor processor,
            ISubscriptionManager subscriptionManager,
            ILogger<MarketDataController> logger)
        {
            _processor = processor ?? throw new ArgumentNullException(nameof(processor));
            _subscriptionManager = subscriptionManager ?? throw new ArgumentNullException(nameof(subscriptionManager));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// 发布JSON格式行情数据
        /// </summary>
        /// <param name="request">JSON数据</param>
        [HttpPost("publish/json")]
        public async Task<IActionResult> PublishJson([FromBody] JsonMarketDataRequest request)
        {
            if (request == null)
                return BadRequest("Request cannot be null");

            try
            {
                // 将JSON对象转换为字节数组
                var jsonData = System.Text.Json.JsonSerializer.Serialize(new
                {
                    symbol = request.Symbol,
                    price = request.Price,
                    volume = request.Volume,
                    seq = request.SequenceNumber,
                    exchange = request.Exchange
                });

                var data = System.Text.Encoding.UTF8.GetBytes(jsonData);

                await _processor.PublishAsync(data, ProtocolType.Json);

                _logger.LogDebug("Published JSON data for {Symbol}", request.Symbol);

                return Ok(new
                {
                    Success = true,
                    Message = $"Published {request.Symbol} at {request.Price}",
                    Timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to publish JSON data");
                return StatusCode(500, new { Success = false, Error = ex.Message });
            }
        }

        ///// <summary>
        ///// 发布二进制格式行情数据
        ///// </summary>
        ///// <param name="request">二进制数据（Base64编码）</param>
        //[HttpPost("publish/binary")]
        //public async Task<IActionResult> PublishBinary([FromBody] BinaryMarketDataRequest request)
        //{
        //}

        ///// <summary>
        ///// 发布Protobuf格式行情数据
        ///// </summary>
        ///// <param name="request">Protobuf数据（Base64编码）</param>
        //[HttpPost("publish/protobuf")]
        //public async Task<IActionResult> PublishProtobuf([FromBody] BinaryMarketDataRequest request)
        //{
        //}

        /// <summary>
        /// 创建新订阅
        /// </summary>
        /// <param name="request">订阅请求</param>
        [HttpPost("subscribe")]
        public IActionResult Subscribe([FromBody] SubscribeRequest request)
        {
            if (request == null || string.IsNullOrEmpty(request.Pattern))
                return BadRequest("Pattern cannot be empty");

            try
            {
                var subscription = new Core.Models.Subscription
                {
                    Pattern = request.Pattern,
                    CallbackUrl = request.CallbackUrl ?? "",
                    Metadata = request.Metadata ?? new Dictionary<string, object>()
                };

                var result = _subscriptionManager.Subscribe(subscription);

                if (result.Success)
                {
                    _logger.LogInformation("Created subscription {SubscriptionId} for pattern {Pattern}",
                        result.SubscriptionId, request.Pattern);

                    return Ok(new
                    {
                        Success = true,
                        SubscriptionId = result.SubscriptionId,
                        Message = $"Subscribed to pattern: {request.Pattern}"
                    });
                }
                else
                {
                    return BadRequest(new { Success = false, Error = result.Error });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create subscription");
                return StatusCode(500, new { Success = false, Error = ex.Message });
            }
        }

        /// <summary>
        /// 取消订阅
        /// </summary>
        /// <param name="subscriptionId">订阅ID</param>
        [HttpDelete("unsubscribe/{subscriptionId}")]
        public IActionResult Unsubscribe(string subscriptionId)
        {
            if (string.IsNullOrEmpty(subscriptionId))
                return BadRequest("SubscriptionId cannot be empty");

            try
            {
                var result = _subscriptionManager.Unsubscribe(subscriptionId);

                if (result)
                {
                    _logger.LogInformation("Unsubscribed {SubscriptionId}", subscriptionId);
                    return Ok(new { Success = true, Message = $"Unsubscribed {subscriptionId}" });
                }
                else
                {
                    return NotFound(new { Success = false, Error = $"Subscription {subscriptionId} not found" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to unsubscribe {SubscriptionId}", subscriptionId);
                return StatusCode(500, new { Success = false, Error = ex.Message });
            }
        }

        /// <summary>
        /// 获取所有订阅
        /// </summary>
        [HttpGet("subscriptions")]
        public IActionResult GetSubscriptions([FromQuery] string? symbol = null)
        {
            try
            {
                // 简化实现，实际应该从SubscriptionManager获取所有订阅
                return Ok(new { Subscriptions = new List<object>() });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get subscriptions");
                return StatusCode(500, new { Success = false, Error = ex.Message });
            }
        }

        #region Request Models

        /// <summary>
        /// JSON格式行情数据请求
        /// </summary>
        public class JsonMarketDataRequest
        {
            public string Symbol { get; set; } = string.Empty;
            public double Price { get; set; }
            public long Volume { get; set; }
            public long SequenceNumber { get; set; }
            public string Exchange { get; set; } = string.Empty;
        }

        /// <summary>
        /// 二进制格式行情数据请求
        /// </summary>
        public class BinaryMarketDataRequest
        {
            public string Data { get; set; } = string.Empty; // Base64 encoded
            public string? Symbol { get; set; } // Optional, for logging
        }

        /// <summary>
        /// 订阅请求
        /// </summary>
        public class SubscribeRequest
        {
            public string Pattern { get; set; } = string.Empty;
            public string? CallbackUrl { get; set; }
            public Dictionary<string, object>? Metadata { get; set; }
        }
        #endregion
    }
}