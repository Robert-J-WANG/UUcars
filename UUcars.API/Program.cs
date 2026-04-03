using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;
using Serilog;
using UUcars.API.Auth;
using UUcars.API.Data;
using UUcars.API.Middleware;

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
    // 后续需要 JWT 配置的地方通过 IOptions<JwtSettings> 注入
    builder.Services.Configure<JwtSettings>(
        builder.Configuration.GetSection("JwtSettings")
    );
    
    // 注册 AppDbContext
    // 从配置文件读取连接字符串（开发环境从 User Secrets 读取）
    builder.Services.AddDbContext<AppDbContext>(options =>
        options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"))
    );

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

    app.UseHttpsRedirection();
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