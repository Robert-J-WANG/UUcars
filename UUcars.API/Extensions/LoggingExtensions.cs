using Microsoft.ApplicationInsights.Extensibility;
using Serilog;
using Serilog.Sinks.ApplicationInsights.TelemetryConverters;

namespace UUcars.API.Extensions;

public static class LoggingExtensions
{
    public static IServiceCollection AddUUcarsApplicationInsights(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString =
            configuration["ApplicationInsights:ConnectionString"];

        services.AddApplicationInsightsTelemetry(options =>
        {
            options.ConnectionString = connectionString;
        });

        return services;
    }

    public static IHostBuilder UseUUcarsSerilog(this IHostBuilder host)
    {
        return host.UseSerilog((context, services, configuration) =>
        {
            configuration
                .ReadFrom.Configuration(context.Configuration)
                .ReadFrom.Services(services)
                .WriteTo.Console()
                .WriteTo.File(
                    "logs/uucars-.log",
                    rollingInterval: RollingInterval.Day,
                    retainedFileCountLimit: 30);

            var applicationInsightsConnectionString =
                context.Configuration["ApplicationInsights:ConnectionString"];

            if (string.IsNullOrWhiteSpace(applicationInsightsConnectionString))
            {
                return;
            }

            var telemetryConfiguration =
                services.GetService<TelemetryConfiguration>();

            if (telemetryConfiguration is null)
            {
                Console.WriteLine(
                    "[WARNING] ApplicationInsights ConnectionString 已配置，" +
                    "但 TelemetryConfiguration 服务未成功注册，AI 日志功能未启用。");
                return;
            }

            configuration.WriteTo.ApplicationInsights(
                telemetryConfiguration,
                new TraceTelemetryConverter());
        });
    }
}
