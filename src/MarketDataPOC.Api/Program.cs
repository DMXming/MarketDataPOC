using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi;
using MarketDataPOC.Adapters;
using MarketDataPOC.Core.Abstractions;
using MarketDataPOC.Core.Processing;
using MarketDataPOC.Subscriptions;

namespace MarketDataPOC.Api
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // 配置应用程序
            ConfigureBuilder(builder);

            var app = builder.Build();

            // 配置HTTP管道
            ConfigurePipeline(app);

            // 启动应用程序
            app.Run();
        }

        private static void ConfigureBuilder(WebApplicationBuilder builder)
        {
            // 添加配置文件
            builder.Configuration
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true)
                .AddEnvironmentVariables("MARKETDATA_");

            // 配置服务
            ConfigureServices(builder.Services, builder.Configuration, builder.Environment);
        }

        private static void ConfigureServices(
            IServiceCollection services,
            IConfiguration configuration,
            IHostEnvironment environment)
        {
            // 添加控制器
            services.AddControllers();

            // 注册核心服务
            services.AddSingleton<ISubscriptionManager, SubscriptionManager>();
            services.AddSingleton<IMarketDataProcessor, MarketDataProcessor>();

            // 注册协议适配器
            services.AddSingleton<IProtocolAdapter, JsonAdapter>();
            services.AddSingleton<IProtocolAdapter, BinaryAdapter>();
            services.AddSingleton<IProtocolAdapter, ProtobufAdapter>();

            // 添加Swagger
            services.AddEndpointsApiExplorer();
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo
                {
                    Title = "Market Data POC API",
                    Version = "v1",
                    Description = "MarketDataPOC",
                });
            });
        }

        private static void ConfigurePipeline(WebApplication app)
        {
            // 异常处理
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();

                // Configure the UI and the endpoint explicitly
                app.UseSwaggerUI(c =>
                {
                    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Market Data POC API v1");
                    c.RoutePrefix = string.Empty;
                });

                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/error");
                app.UseHsts();
            }

            // 安全中间件
            app.UseHttpsRedirection();

            // 路由
            app.UseRouting();

            // 认证授权
            app.UseAuthentication();
            app.UseAuthorization();

            // 控制器映射
            app.MapControllers();

            // 回退端点
            app.MapFallback(async context =>
            {
                context.Response.StatusCode = 404;
                await context.Response.WriteAsJsonAsync(new
                {
                    Error = "Not Found",
                    Message = "The requested endpoint does not exist"
                });
            });

            // 启动日志
            var logger = app.Services.GetRequiredService<ILogger<Program>>();
            logger.LogInformation("Market Data POC API started at {Time}", DateTime.UtcNow);
            logger.LogInformation("Environment: {Environment}", app.Environment.EnvironmentName);
            logger.LogInformation("Listening URLs: {Urls}", string.Join(", ", app.Urls));
        }
    }
}