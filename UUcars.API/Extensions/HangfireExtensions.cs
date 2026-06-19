using Hangfire;
using Hangfire.SqlServer;
using UUcars.API.Services.Hangfire;

namespace UUcars.API.Extensions;

public static class HangfireExtensions
{
    public static IServiceCollection AddHangfireServices(this IServiceCollection services, IConfiguration configuration,
        IWebHostEnvironment environment)
    {
        // ✅ Testing 环境完全跳过 Hangfire 配置
        // 原因：AddHangfire 的 UseSqlServerStorage 会尝试连接数据库初始化系统表
        // Testcontainers 的数据库里没有 Hangfire 系统表，直接崩溃
        // Testing 环境只需要 FakeBackgroundJobClient（在 SqlServerTestFactory 里注册）
        if (environment.IsEnvironment("Testing"))
            return services;

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

        return services;
    }

    // 注册定时任务（Recurring Jobs）
    // 在 app.UseHangfire() 里调用，确保在应用完全启动后执行
    public static void UseHangfireJobs(this IApplicationBuilder app)
    {
        // 过期 Token 清理任务
        // Cron.Daily = 每天凌晨 00:00 执行
        // 凌晨执行原因：流量最低，对用户无感知
        RecurringJob.AddOrUpdate<TokenCleanupService>(
            "cleanup-expired-tokens", // 任务 ID（唯一标识，方便在 Dashboard 里识别）
            service => service.CleanExpiredTokensAsync(),
            Cron.Daily, // 执行频率
            new RecurringJobOptions
            {
                TimeZone = TimeZoneInfo.Utc // 统一用 UTC，避免时区问题
            }
        );
    }
}