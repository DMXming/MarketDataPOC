using System.Threading;
using System.Threading.Tasks;
using MarketDataPOC.Core.Models;

namespace MarketDataPOC.Core.Abstractions
{
    public interface IMarketDataProcessor
    {
        /// <summary>
        /// 发布原始行情数据
        /// </summary>
        ValueTask PublishAsync(ReadOnlyMemory<byte> data, ProtocolType protocol);

        /// <summary>
        /// 订阅处理事件
        /// </summary>
        IDisposable Subscribe(IObserver<MarketData> observer);

        /// <summary>
        /// 获取当前处理指标
        /// </summary>
        ProcessorMetrics GetMetrics();
    }
}