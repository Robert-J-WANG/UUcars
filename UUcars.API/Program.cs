using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Scalar.AspNetCore;
using Serilog;
using UUcars.API.Auth;
using UUcars.API.Data;
using UUcars.API.Entities;
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
    // 后续需要 JWT 配置的地方通过 IOptions<JwtSettings> 注入
    builder.Services.Configure<JwtSettings>(
        builder.Configuration.GetSection("JwtSettings")
    );

    // 注册 AppDbContext
    // 从配置文件读取连接字符串（开发环境从 User Secrets 读取）
    builder.Services.AddDbContext<AppDbContext>(options =>
        options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"))
    );

    // 配置 JWT 认证
    // 先从配置系统读取 JwtSettings
    // 开发环境自动从 User Secrets 读取真实值（Step 03 已配置）
    var jwtSettings = builder.Configuration.GetSection("JwtSettings").Get<JwtSettings>()!;

    builder.Services.AddAuthentication(options =>
        {
            // DefaultAuthenticateScheme：指定默认用哪种方式来"认证"请求
            // DefaultChallengeScheme：当认证失败时，用哪种方式来"挑战"客户端
            //   （对 JWT 来说，挑战的结果就是返回 401 Unauthorized）
            // 两个都设为 JwtBearerDefaults.AuthenticationScheme（值是字符串 "Bearer"），
            // 表示默认用 JWT Bearer 方式处理认证，这样 [Authorize] 特性不需要额外指定方案
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            // TokenValidationParameters：告诉框架"验证 Token 时要检查哪些东西"
            options.TokenValidationParameters = new TokenValidationParameters
            {
                // ===== 签名验证 =====
                // ValidateIssuerSigningKey = true：必须验证签名
                // IssuerSigningKey：用来验证签名的 Key，必须和生成 Token 时用的 Key 一样
                // 原理：框架用这个 Key 对 Token 的 Header+Payload 重新计算签名，
                //       和 Token 第三段比对，不一样就拒绝
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(
                    Encoding.UTF8.GetBytes(jwtSettings.Secret)),

                // ===== Issuer 验证 =====
                // 验证 Token 的 iss 字段是否匹配，防止接受其他系统签发的 Token
                ValidateIssuer = true,
                ValidIssuer = jwtSettings.Issuer, // 值："UUcars"

                // ===== Audience 验证 =====
                // 验证 Token 的 aud 字段是否匹配，确保 Token 是给我们的系统用的
                ValidateAudience = true,
                ValidAudience = jwtSettings.Audience, // 值："UUcarsUsers"

                // ===== 过期时间验证 =====
                // 验证 Token 的 exp 字段，过期的 Token 直接拒绝
                ValidateLifetime = true,

                // ===== 时钟偏差 =====
                // 默认值是 5 分钟：即使 Token 过期了，还有 5 分钟的宽限期
                // 设为 0 更严格：过期就是过期，没有宽限期
                // 宽限期的存在是为了应对不同服务器之间时钟不完全同步的情况，
                // 单机部署时设为 0 没问题
                ClockSkew = TimeSpan.Zero
            };
        });


    // 用户模块
    // AddScoped：每次 HTTP 请求创建一个新实例，请求结束后销毁
    // Repository 和 Service 都用 Scoped，因为它们依赖 DbContext（也是 Scoped）
    builder.Services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();
    builder.Services.AddScoped<IUserRepository, EfUserRepository>();
    builder.Services.AddScoped<JwtTokenGenerator>();
    builder.Services.AddScoped<UserService>();

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