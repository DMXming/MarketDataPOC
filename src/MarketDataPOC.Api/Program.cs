using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MarketDataPOC.Core.Processing;
using MarketDataPOC.Core.Abstractions;
using MarketDataPOC.Core.Pooling;
using MarketDataPOC.Subscription;
using MarketDataPOC.Adapters;

var builder = WebApplication.CreateBuilder(args);

// Services
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
//builder.Services.AddSwaggerGen();

// Register core services with simple lifetimes for the POC
builder.Services.AddSingleton<IMarketDataProcessor, MarketDataProcessor>();
builder.Services.AddSingleton<MarketDataPool>();
builder.Services.AddSingleton<ArrayPoolBuffer>();
builder.Services.AddSingleton<IProtocolAdapter, JsonAdapter>();
builder.Services.AddSingleton<SubscriptionManager>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    //app.UseSwagger();
    //app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "MarketDataPOC API v1"));
}

app.MapControllers();

app.Run();