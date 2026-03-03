# MarketDataPOC

Proof-of-concept project for market data ingestion, processing and subscription management.

Structure:
- src/MarketDataPOC.Api ！ Web API entrypoint and controllers
- src/MarketDataPOC.Core ！ Core models, abstractions, processing and pooling
- src/MarketDataPOC.Adapters ！ Protocol adapters (JSON/Binary/Protobuf stubs)
- src/MarketDataPOC.Subscription ！ Subscription management and topic matching
- src/MarketDataPOC.Simulator ！ Simple data generator
- tests ！ Unit, integration and benchmark test projects (skeletons)

Quick start:
1. Open the solution in Visual Studio or run:
   - `dotnet restore`
   - `dotnet build`
   - `dotnet run --project src/MarketDataPOC.Api/MarketDataPOC.Api.csproj`
2. API will be available on the configured port (see docker-compose for containerized example).

Formatting:
- Run the Visual Studio command __Format Document__ before committing.
- Project rules are enforced by `.editorconfig`.

Contributing:
- See `CONTRIBUTING.md` for branch, commit and PR guidelines.