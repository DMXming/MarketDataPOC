using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using MarketDataPOC.Core.Abstractions;

namespace MarketDataPOC.Api.Middleware
{
    /// <summary>
    /// 指标收集中间件
    /// </summary>
    public class MetricsMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<MetricsMiddleware> _logger;
        private static readonly Counter _requestCounter = new();
        private static readonly Histogram _requestDuration = new();

        public MetricsMiddleware(RequestDelegate next, ILogger<MetricsMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context, IMarketDataProcessor processor)
        {
            var stopwatch = Stopwatch.StartNew();
            var path = context.Request.Path;
            var method = context.Request.Method;

            try
            {
                // 调用下一个中间件
                await _next(context);

                stopwatch.Stop();

                // 记录请求指标
                _requestCounter.Increment(method, path, context.Response.StatusCode.ToString());
                _requestDuration.Observe(stopwatch.Elapsed.TotalMilliseconds, method, path);

                // 每100个请求记录一次日志
                if (_requestCounter.GetCount() % 100 == 0)
                {
                    var metrics = processor.GetMetrics();
                    _logger.LogInformation(
                        "Request metrics - Method: {Method}, Path: {Path}, Status: {StatusCode}, Duration: {DurationMs}ms, " +
                        "Processor - Processed: {Processed}, Queue: {Queue}",
                        method, path, context.Response.StatusCode, stopwatch.Elapsed.TotalMilliseconds,
                        metrics.ProcessedCount, metrics.QueueLength);
                }
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex, "Request failed - {Method} {Path}", method, path);
                throw;
            }
        }

        #region Simple Metrics Classes

        private class Counter
        {
            private long _count;
            private readonly object _lock = new();

            public void Increment(string method, string path, string statusCode)
            {
                lock (_lock)
                {
                    _count++;
                }
            }

            public long GetCount()
            {
                lock (_lock)
                {
                    return _count;
                }
            }
        }

        private class Histogram
        {
            private readonly List<double> _samples = new();
            private readonly object _lock = new();

            public void Observe(double value, string method, string path)
            {
                lock (_lock)
                {
                    _samples.Add(value);
                    if (_samples.Count > 1000)
                    {
                        _samples.RemoveAt(0);
                    }
                }
            }

            public double GetAverage()
            {
                lock (_lock)
                {
                    return _samples.Count > 0 ? _samples.Average() : 0;
                }
            }
        }

        #endregion
    }
}