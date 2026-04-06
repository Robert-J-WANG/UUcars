using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using UUcars.API.Auth;

namespace UUcars.API.Extensions;

public static class AuthExtensions
{
    // 不能通过 IOptions<JwtSettings> 注入的方法使用jwtSettings配置
    // 因为AddJwtAuthentication的功能还在注册阶段，不是在运行阶段，无法通过DI创建JwtSettings对象实例
    /*
     public static IServiceCollection AddJwtAuthentication(this IServiceCollection services,
        IOptions<JwtSettings> jwtSettings)
    {
        return services;
    }
    */

    // 先从配置系统读取 JwtSettings
    // // 开发环境自动从 User Secrets 读取真实值（Step 03 已配置）
    public static IServiceCollection AddJwtAuthentication(this IServiceCollection services,
        IConfiguration configuration)
    {
        var jwtSettings = configuration.GetSection("JwtSettings").Get<JwtSettings>()!;
        services.AddAuthentication(options =>
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
        
        return services;
    }
}