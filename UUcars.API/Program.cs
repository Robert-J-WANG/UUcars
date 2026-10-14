using Hangfire;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;
using Serilog;
using UUcars.API.Auth;
using UUcars.API.Auth.Hangfire;
using UUcars.API.Configurations;
using UUcars.API.Data;
using UUcars.API.Entities;
using UUcars.API.Extensions;
using UUcars.API.Hubs;
using UUcars.API.Middleware;
using UUcars.API.Repositories;
using UUcars.API.Services;
using UUcars.API.Services.Audit;
using UUcars.API.Services.ExternalLogin;
using UUcars.API.Services.Notifications;

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
    // 让本地 E2E 环境显式读取同 User Secrets 配置
    if (builder.Environment.IsEnvironment(AppEnvironments.E2E)) builder.Configuration.AddUserSecrets<Program>();

    // Serilog日志
    builder.Host.UseUUcarsSerilog();

    // =============================================
    // 读取启动所需配置
    // =============================================

    // 根据当前环境选择数据库连接名称
    var databaseConnectionName =
        builder.Environment.IsEnvironment(AppEnvironments.E2E)
            ? "E2EConnection"
            : "DefaultConnection";

    var databaseConnectionString = builder.Configuration
        .GetConnectionString(databaseConnectionName);

    if (string.IsNullOrWhiteSpace(databaseConnectionString))
        throw new InvalidOperationException(
            $"Connection string '{databaseConnectionName}' is not configured.");

    var jwtSection = builder.Configuration
        .GetRequiredSection("JwtSettings");

    builder.Services.Configure<JwtSettings>(jwtSection);
    builder.Services.Configure<GoogleAuthSettings>(
        builder.Configuration.GetSection("GoogleAuth"));

    // =============================================
    // ASP.NET Core 框架能力
    // =============================================
    builder.Services.AddControllers();
    builder.Services.AddOpenApi();
    builder.Services.AddUUcarsCors(builder.Configuration);
    builder.Services.AddJwtAuthentication(jwtSection);
    builder.Services.AddRateLimiting();
    builder.Services.AddSignalR();
    builder.Services.AddHttpContextAccessor();

    // =============================================
    // 基础设施
    // =============================================
    builder.Services.AddDbContext<AppDbContext>(options =>
        options.UseSqlServer(databaseConnectionString));
    builder.Services.AddEmailService(builder.Configuration);
    builder.Services.AddStorageService(builder.Configuration);
    builder.Services.AddCacheServices(builder.Configuration);
    builder.Services.AddHangfireServices(
        builder.Configuration,
        builder.Environment);
    builder.Services.AddSingleton<TimeProvider>(TimeProvider.System);
    builder.Services.AddUUcarsApplicationInsights(builder.Configuration);

    // =============================================
    // 应用与业务服务
    // =============================================
    // 用户模块
    builder.Services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();
    builder.Services.AddScoped<IUserRepository, EfUserRepository>();
    builder.Services.AddScoped<JwtTokenGenerator>();
    builder.Services.AddScoped<UserService>();
    builder.Services.AddScoped<CurrentUserService>();

    // 车辆模块
    builder.Services.AddScoped<ICarRepository, EfCarRepository>();
    builder.Services.AddScoped<ICarImageRepository, EfCarImageRepository>();
    builder.Services.AddScoped<CarService>();

    // 收藏模块
    builder.Services.AddScoped<IFavoriteRepository, EfFavoriteRepository>();
    builder.Services.AddScoped<FavoriteService>();

    // 订单模块
    builder.Services.AddScoped<IOrderRepository, EfOrderRepository>();
    builder.Services.AddScoped<OrderService>();

    // Admin 模块
    builder.Services.AddScoped<AdminCarService>();
    builder.Services.AddScoped<IAuditLogService, AuditLogService>();

    // 评价模块
    builder.Services.AddScoped<IReviewRepository, EfReviewRepository>();
    builder.Services.AddScoped<ReviewService>();

    // Refresh Token 模块
    builder.Services.AddScoped<IRefreshTokenRepository, EfRefreshTokenRepository>();
    builder.Services.AddScoped<RefreshTokenService>();

    // Notification 模块
    builder.Services.AddScoped<INotificationRepository, EfNotificationRepository>();
    builder.Services.AddScoped<INotificationService, NotificationService>();

    // Admin Dashboard 模块
    builder.Services.AddScoped<IAdminStatsRepository, EfAdminStatsRepository>();
    builder.Services.AddScoped<AdminStatsService>();

    // 外部登录模块
    builder.Services.AddScoped<IGoogleIdTokenService, GoogleIdTokenService>();
    builder.Services.AddScoped<IExternalLoginRepository, EfExternalLoginRepository>();
    builder.Services.AddScoped<ExternalLoginService>();

    // =============================================
    // 构建应用
    // =============================================
    var app = builder.Build();

    // =============================================
    // 启动初始化
    // =============================================
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

                await Task.Delay(3000);
            }
    }

    var backgroundJobsEnabled =
        !app.Environment.IsEnvironment(AppEnvironments.Testing) &&
        !app.Environment.IsEnvironment(AppEnvironments.E2E);

    if (backgroundJobsEnabled) app.UseBackgroundJobs();

    // =============================================
    // 中间件管道
    // =============================================
    app.UseMiddleware<GlobalExceptionMiddleware>();
    app.UseSecurityHeaders();
    app.UseCors(CorsExtensions.PolicyName);
    app.UseAuthentication();
    app.UseRateLimiter();
    app.UseAuthorization();

    if (backgroundJobsEnabled)
        app.UseHangfireDashboard("/hangfire", new DashboardOptions
        {
            Authorization = app.Environment.IsDevelopment()
                ? new[] { new HangfireLocalDevAuthorizationFilter() }
                : new[] { new HangfireAdminAuthorizationFilter() }
        });

    // =============================================
    // Endpoints
    // =============================================
    if (app.Environment.IsDevelopment())
    {
        app.MapOpenApi();
        app.MapScalarApiReference(options =>
            options
                .WithTitle("UUcars API Documentation")
                .WithTheme(ScalarTheme.Moon));
    }

    app.MapControllers();
    app.MapHub<NotificationHub>("/hubs/notification");

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly during startup");
    throw;
}
finally
{
    Log.CloseAndFlush();
}

// 让 Program 类对测试项目可见
// WebApplicationFactory<Program> 需要通过这个类找到应用入口
public partial class Program
{
}