# MarketDataPOC

Proof-of-concept project for market data ingestion, processing and subscription management.

Structure:
- src/MarketDataPOC.Api — Web API entrypoint and controllers
- src/MarketDataPOC.Core — Core models, abstractions, processing and pooling
- src/MarketDataPOC.Adapters — Protocol adapters (JSON/Binary/Protobuf stubs)
- src/MarketDataPOC.Subscription — Subscription management and topic matching
- src/MarketDataPOC.Simulator — Simple data generator
- tests — Unit, integration and benchmark test projects (skeletons)

Quick start:
1. Open the solution in Visual Studio or run:
   - `dotnet restore`
   - `dotnet build`
   - `dotnet run --project src/MarketDataPOC.Api/MarketDataPOC.Api.csproj`
2. API will be available on the configured port (see docker-compose for containerized example).

AI 辅助的部分
代码生成：
  - 基础的Controller和DTO结构
  - 单元测试框架和测试用例
  - Dockerfile和docker-compose配置
  - README文档框架

人工调整与改进
- 内存管理优化：
  - AI生成的代码未正确处理对象池的归还，添加了try-finally确保归还
  - 调整了ArrayPool的租赁策略，避免过大的缓冲区分配
  - 实现了自定义的对象池策略，减少锁竞争
- 并发控制：
  - 重写了Channel的消费逻辑，支持批量处理
  - 添加了背压监控和动态调整机制
  - 优化了Parallel.ForEach的分区策略
- 错误处理：
  - 增加了消息解析失败的重试机制
  - 实现了降级策略（当Channel满时的处理）
  - 添加了详细的性能计数器
- 可观测性：
  - 集成了metrics
  - 详细日志 - 各级别日志记录
  - 性能指标 - 处理速度、错误率、延迟等
  - 死信监控 - 失败消息的追踪
  - 状体统计 - 活跃标的、消息计数等
