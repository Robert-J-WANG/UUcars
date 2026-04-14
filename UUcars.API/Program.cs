using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Scalar.AspNetCore;
using Serilog;
using UUcars.API.Auth;
using UUcars.API.Data;
using UUcars.API.Entities;
using UUcars.API.Extensions;
using UUcars.API.Middleware;
using UUcars.API.Repositories;
using UUcars.API.Services;

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
        configuration
            .ReadFrom.Configuration(context.Configuration)
            .ReadFrom.Services(services)
            .WriteTo.Console()
            .WriteTo.File(
                path: "logs/uucars-.log",
                rollingInterval: RollingInterval.Day, // 每天滚动一个新文件
                retainedFileCountLimit: 30 // 最多保留 30 天
            )
    );

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
    
    // 用户模块
    // AddScoped：每次 HTTP 请求创建一个新实例，请求结束后销毁
    // Repository 和 Service 都用 Scoped，因为它们依赖 DbContext（也是 Scoped）
    builder.Services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();
    builder.Services.AddScoped<IUserRepository, EfUserRepository>();
    builder.Services.AddScoped<JwtTokenGenerator>();
    builder.Services.AddScoped<UserService>();
    
    // AddHttpContextAccessor：IHttpContextAccessor 默认不自动注册，需要显式加上
    // 它内部用 AsyncLocal<T> 保证线程安全，每个请求有自己独立的 HttpContext
    builder.Services.AddHttpContextAccessor();
    builder.Services.AddScoped<CurrentUserService>();
    
    // 车辆模块
    builder.Services.AddScoped<ICarRepository, EfCarRepository>();
    builder.Services.AddScoped<CarService>();
    builder.Services.AddScoped<ICarImageRepository, EfCarImageRepository>();  // 新增
    
    // 收藏模块
    builder.Services.AddScoped<IFavoriteRepository, EfFavoriteRepository>();
    builder.Services.AddScoped<FavoriteService>();
    
    // 订单模块
    builder.Services.AddScoped<IOrderRepository, EfOrderRepository>();
    builder.Services.AddScoped<OrderService>();
    
    // Admin 模块
    builder.Services.AddScoped<AdminCarService>();

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

    // UseAuthentication 必须在 UseAuthorization 之前
    // 原因：Authorization 需要读取 Authentication 的结果（HttpContext.User）
    // 如果顺序反了，[Authorize] 读到的 HttpContext.User 是空的，永远判定为未认证
    app.UseAuthentication();
    app.UseAuthorization();

    app.MapControllers();

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