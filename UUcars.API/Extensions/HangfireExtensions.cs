using Hangfire;
using Hangfire.SqlServer;
using UUcars.API.Configurations;
using UUcars.API.Services.BackgroundJobs;

namespace UUcars.API.Extensions;

public static class HangfireExtensions
{
    public static IServiceCollection AddHangfireServices(this IServiceCollection services, IConfiguration configuration,
        IWebHostEnvironment environment)
    {
        // Testing 环境由 Integration Test Factory 注册 FakeBackgroundJobService。
        if (environment.IsEnvironment(AppEnvironments.Testing))
            return services;

        if (environment.IsEnvironment(AppEnvironments.E2E))
        {
            services.AddScoped<IBackgroundJobService, E2EBackgroundJobService>();
            return services;
        }

        // 获取数据库连接符
        var connectionString = configuration.GetConnectionString("DefaultConnection") ??
                               throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
        // 连接数据库
        services.AddHangfire(config => config
            // 使用 SQL Server 作为存储后端
            .UseSqlServerStorage(connectionString, new SqlServerStorageOptions
            {
                // 队列轮询间隔：Hangfire Server 多久检查一次队列
                // 默认 15 秒，对邮件发送来说可以接受
                // 如果需要更快响应，可以降到 5 秒
                QueuePollInterval = TimeSpan.FromSeconds(15),

                // 任务滑动不可见超时：任务被取走后多久没有完成算超时
                // 超时后重新放回队列（防止 Worker 崩溃导致任务永久丢失）
                SlidingInvisibilityTimeout = TimeSpan.FromMinutes(5), // 官方推荐显式设置

                // 自动删除已完成的任务（保留 7 天，避免数据库无限增长）
                JobExpirationCheckInterval = TimeSpan.FromHours(1)
            })
            // 使用结构化日志（接入已有的 Serilog）
            .UseRecommendedSerializerSettings()
        );


        // 注册 Hangfire Server（在当前进程里运行后台 Worker）
        services.AddHangfireServer(options =>
        {
            // Worker 线程数：同时处理多少个任务
            // 默认是 CPU 核心数 * 5，对我们足够
            options.WorkerCount = 5;

            // 队列名称：Worker 只处理哪些队列
            // 默认只处理 "default" 队列，我们暂时只用这一个
            options.Queues = new[] { "default" };
        });

        services.AddScoped<IBackgroundJobService, HangfireBackgroundJobService>();

        return services;
    }

    public static void UseBackgroundJobs(this IApplicationBuilder app)
    {
        using var scope = app.ApplicationServices.CreateScope();
        var backgroundJobService = scope.ServiceProvider
            .GetRequiredService<IBackgroundJobService>();

        backgroundJobService.RegisterRecurringJobs();
    }
}
