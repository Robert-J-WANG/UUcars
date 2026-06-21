using Hangfire;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Net.Http.Headers;
using Scalar.AspNetCore;
using Serilog;
using Serilog.Sinks.ApplicationInsights.TelemetryConverters; // ← 新增
using StackExchange.Redis;
using UUcars.API.Auth;
using UUcars.API.Auth.Hangfire;
using UUcars.API.Data;
using UUcars.API.Entities;
using UUcars.API.Extensions;
using UUcars.API.Middleware;
using UUcars.API.Repositories;
using UUcars.API.Services;
using UUcars.API.Services.Audit;
using UUcars.API.Services.Cache;
using UUcars.API.Services.Email;
using UUcars.API.Services.Storage;

// =============================================
// Bootstrap Logger
// 在 builder 构建之前就启动一个临时 Logger
// 用途：如果 builder 阶段本身出错（比如配置文件格式错误），
//       也能把错误信息记录下来
// =============================================
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);


    // =============================================
    // 替换默认日志系统为 Serilog
    // ReadFrom.Configuration：从 appsettings.json 读取日志级别等配置
    // WriteTo.Console / File：输出目标
    // =============================================

    builder.Host.UseSerilog((context, services, configuration) =>
    {
        configuration
            .ReadFrom.Configuration(context.Configuration)
            .ReadFrom.Services(services)
            .WriteTo.Console()
            .WriteTo.File(
                "logs/uucars-.log",
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 30
            );

        // V3 修复：原本用 GetRequiredService<TelemetryConfiguration>()，
        // 一旦该服务未被成功注册就会直接抛 InvalidOperationException，
        // 导致 builder.Build() 失败、应用启动崩溃（2026-06 在 Container App
        // 上线后实际发生过一次，根因是订阅状态变动后 AI SDK 的服务注册
        // 出现了异常，具体内部原因未完全定位，详见 PR 描述）。
        //
        // 改用 GetService（允许返回 null）+ 判空，把"是否启用 Application
        // Insights 日志"降级为非关键路径：即使该服务因为任何原因未注册成功，
        // 应用本身依然能正常启动并提供核心业务功能，只是会失去 AI 日志能力。
        //
        // else 分支打印 Console 警告，是为了避免这种"静默失效"难以察觉——
        // 万一以后又复现类似问题，至少能在 Container App 的 Console log
        // stream 里看到提示，而不是悄无声息地丢失可观测性却毫无察觉。
        var aiConnStr = context.Configuration["ApplicationInsights:ConnectionString"];
        if (!string.IsNullOrEmpty(aiConnStr))
        {
            var telemetryConfig = services.GetService
                <Microsoft.ApplicationInsights.Extensibility.TelemetryConfiguration>();

            if (telemetryConfig != null)
                configuration.WriteTo.ApplicationInsights(telemetryConfig, new TraceTelemetryConverter());
            else
                Console.WriteLine("[WARNING] ApplicationInsights ConnectionString 已配置，" +
                                  "但 TelemetryConfiguration 服务未成功注册，AI 日志功能未启用。");
        }
    });


    // =============================================
    // 服务注册
    // =============================================
    builder.Services.AddControllers();

    // OpenAPI + Scalar（API 文档）
    builder.Services.AddOpenApi();

    // 将 appsettings.json 中的 JwtSettings 节绑定到 JwtSettings 类
    // 后续 运行阶段 需要 JWT 配置的地方通过 IOptions<JwtSettings> 注入
    builder.Services.Configure<JwtSettings>(
        builder.Configuration.GetSection("JwtSettings")
    );

    // 注册 AppDbContext
    // 从配置文件读取连接字符串（开发环境从 User Secrets 读取）
    builder.Services.AddDbContext<AppDbContext>(options =>
        options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"))
    );

    // 配置 JWT 认证
    // JWT 认证（细节在 Extensions/AuthExtensions.cs）
    builder.Services.AddJwtAuthentication(builder.Configuration);

    // CORS 配置
    builder.Services.AddUUcarsCors(builder.Configuration);

    // 配置邮件服务
    builder.Services.AddEmailService(builder.Configuration);

    // 存储服务
    builder.Services.AddStorageService(builder.Configuration);

    // ── Application Insights 服务 ──
    // V3 修复：原本用 if (!string.IsNullOrEmpty(aiConnectionString)) 包裹，
    // 只在配置了 ConnectionString 时才注册，目的是避免本地开发环境
    // 把测试日志发到 Azure，同时省去不必要的初始化开销。
    //
    // 但这种"按需注册"模式存在隐患：UseSerilog 的 lambda 里（见下方）
    // 同样独立判断了一次 ConnectionString 是否为空，两边的判断理论上该同步，
    // 但 AddApplicationInsightsTelemetry() 内部的注册逻辑不完全透明，
    // 实测出现过"传入了合法的 ConnectionString，却没有把
    // TelemetryConfiguration 真正注册进 DI 容器"的情况，导致下游
    // GetRequiredService<TelemetryConfiguration> 直接抛异常，整个应用启动崩溃。
    //
    // 改为无条件注册：AddApplicationInsightsTelemetry 即使传入空字符串
    // 也不会报错，只是会处于"禁用遥测"状态（不发送数据），但依然会把
    // TelemetryConfiguration 服务注册进容器。这样可以从根上保证该服务
    // 一定存在，避免"注册条件"和"使用条件"不同步导致的崩溃。
    var aiConnectionString = builder.Configuration["ApplicationInsights:ConnectionString"];
    builder.Services.AddApplicationInsightsTelemetry(options => { options.ConnectionString = aiConnectionString; });

    // ── Redis Cache服务 ──

    // Redis 连接（IConnectionMultiplexer 是线程安全的，注册为 Singleton）
    // Singleton：整个应用生命周期只创建一次连接，所有请求共享
    // 不用 Scoped 的原因：Redis 连接是昂贵的资源，每次请求创建一个连接会耗尽连接池
    var redisConnectionString = builder.Configuration
        .GetConnectionString("Redis") ?? "localhost:6379";

    builder.Services.AddSingleton<IConnectionMultiplexer>(
        ConnectionMultiplexer.Connect(redisConnectionString));

    // IDistributedCache 的 Redis 实现
    builder.Services.AddStackExchangeRedisCache(options =>
    {
        options.Configuration = redisConnectionString;
        // Key 前缀：区分不同应用共用同一个 Redis 实例时的 Key 冲突
        options.InstanceName = "uucars:";
    });

    // 限流策略服务 - 自定义的扩展方法
    builder.Services.AddRateLimiting();


    // 用户模块
    // AddScoped：每次 HTTP 请求创建一个新实例，请求结束后销毁
    // Repository 和 Service 都用 Scoped，因为它们依赖 DbContext（也是 Scoped）
    builder.Services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();
    builder.Services.AddScoped<IUserRepository, EfUserRepository>();
    builder.Services.AddScoped<JwtTokenGenerator>();
    builder.Services.AddScoped<UserService>();
    // 邮件服务
    builder.Services.AddScoped<IEmailService, ResendEmailService>();
    // 注册我们自己的 IStorageService 实现
    builder.Services.AddScoped<IStorageService, R2StorageService>();

    // AddHttpContextAccessor：IHttpContextAccessor 默认不自动注册，需要显式加上
    // 它内部用 AsyncLocal<T> 保证线程安全，每个请求有自己独立的 HttpContext
    builder.Services.AddHttpContextAccessor();
    builder.Services.AddScoped<CurrentUserService>();

    // 缓存服务（Singleton，因为它依赖 Singleton 的 IConnectionMultiplexer）
    builder.Services.AddSingleton<ICacheService, RedisCacheService>();

    // Hangfire服务
    builder.Services.AddHangfireServices(builder.Configuration, builder.Environment);


    // 车辆模块
    builder.Services.AddScoped<ICarRepository, EfCarRepository>();
    builder.Services.AddScoped<CarService>();
    builder.Services.AddScoped<ICarImageRepository, EfCarImageRepository>(); // 新增

    // 收藏模块
    builder.Services.AddScoped<IFavoriteRepository, EfFavoriteRepository>();
    builder.Services.AddScoped<FavoriteService>();

    // 订单模块
    builder.Services.AddScoped<IOrderRepository, EfOrderRepository>();
    builder.Services.AddScoped<OrderService>();

    // Admin 模块
    builder.Services.AddScoped<AdminCarService>();
    builder.Services.AddScoped<IAuditLogService, AuditLogService>(); // ✅ 新增

    // 评价模块
    builder.Services.AddScoped<IReviewRepository, EfReviewRepository>();
    builder.Services.AddScoped<ReviewService>();

    // ── Refresh Token ──────────────────────────────────
    builder.Services.AddScoped<IRefreshTokenRepository, EfRefreshTokenRepository>();
    builder.Services.AddScoped<RefreshTokenService>();

    // =============================================
    // 构建应用
    // =============================================
    var app = builder.Build();


    // =============================================
    // 中间件管道
    // 顺序很重要：GlobalExceptionMiddleware 必须在最外层
    // =============================================

    // 全局异常处理，放最前面，兜住所有后续中间件的异常
    app.UseMiddleware<GlobalExceptionMiddleware>();

    // ✅ 安全响应头（新增）
    app.UseSecurityHeaders();

    // 开发环境才挂载 API 文档
    if (app.Environment.IsDevelopment())
    {
        app.MapOpenApi();
        app.MapScalarApiReference(options =>
            options
                .WithTitle("UUcars API Documentation")
                .WithTheme(ScalarTheme.Moon)
        );
    }

    // CORS 必须在 Authentication 和 Authorization 之前
    // 原因：Preflight 请求（OPTIONS）不携带 Token，
    // 如果 CORS 在 Auth 之后，Preflight 会因为没有 Token 被拦截，
    // 导致浏览器认为服务器不支持跨域
    app.UseCors(CorsExtensions.PolicyName);

    // ✅ 限流中间件
    // 必须在 UseCors之后
    // 原因：限流策略导致的响应码 429，响应没有 CORS 头，浏览器拦截
    // 必须在 UseAuthentication / UseAuthorization 之前
    // 原因：限流是最外层的防护，不管请求有没有 Token，都要先过限流检查
    // 这样攻击者带着无效 Token 的请求也能被拦截，不会浪费认证处理的开销

    app.UseRateLimiter();

    // UseAuthentication 必须在 UseAuthorization 之前
    // 原因：Authorization 需要读取 Authentication 的结果（HttpContext.User）
    // 如果顺序反了，[Authorize] 读到的 HttpContext.User 是空的，永远判定为未认证
    app.UseAuthentication();
    app.UseAuthorization();

    // ✅ 非 Testing 环境才挂载 Hangfire Dashboard 和定时任务
    if (!app.Environment.IsEnvironment("Testing"))
    {
        // ✅ Hangfire Dashboard（在认证之后）
        app.UseHangfireDashboard("/hangfire", new DashboardOptions
        {
            Authorization = app.Environment.IsDevelopment()
                ? new[] { new HangfireLocalDevAuthorizationFilter() } // 开发：直接放行
                : new[] { new HangfireAdminAuthorizationFilter() } // 生产：必须 Admin
        });
        // 定时清除过期token
        app.UseHangfireJobs();
    }


    app.MapControllers();

    // 自动执行 Migration（V1 学习项目用法）
    // 生产环境建议改为独立的部署脚本，不在应用启动时执行
    using (var scope = app.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var retries = 5;
        while (retries > 0)
            try
            {
                await db.Database.MigrateAsync();
                break;
            }
            catch (Exception)
            {
                retries--;
                if (retries == 0) throw;
                // SQL Server 健康检查刚通过，但有时仍需几秒才能真正接受连接
                // 等待 3 秒后重试，最多重试 5 次
                await Task.Delay(3000);
            }
    }

    app.Run();
}
catch (Exception ex)
{
    // 捕获启动阶段的致命错误（配置错误、数据库连不上等）
    Log.Fatal(ex, "Application terminated unexpectedly during startup");
}
finally
{
    // 确保程序退出前把缓冲区里的日志全部写出去
    Log.CloseAndFlush();
}

// 让 Program 类对测试项目可见
// WebApplicationFactory<Program> 需要通过这个类找到应用入口
public partial class Program
{
};