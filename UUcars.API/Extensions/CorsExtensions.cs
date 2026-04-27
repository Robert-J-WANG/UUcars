namespace UUcars.API.Extensions;

public static class CorsExtensions
{
    // CORS 策略名称，在注册和使用时保持一致
    public const string PolicyName = "UUcarsCorsPolicy";

    public static IServiceCollection AddUUcarsCors(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // 从配置文件读取允许的前端地址列表
        var allowedOrigins = configuration
            .GetSection("Cors:AllowedOrigins")
            .Get<string[]>() ?? [];

        services.AddCors(options =>
        {
            options.AddPolicy(PolicyName, policy =>
            {
                policy
                    // 只允许配置里指定的域名，不用 AllowAnyOrigin
                    // 原因：AllowAnyOrigin 表示任何网站都可以调用你的 API，
                    // 配合 AllowCredentials（允许携带 Cookie）时甚至会报错——
                    // 浏览器要求：如果允许携带凭证，Origin 不能是通配符
                    .WithOrigins(allowedOrigins)

                    // 允许所有 HTTP 方法（GET、POST、PUT、DELETE 等）
                    .AllowAnyMethod()

                    // 允许所有请求头（Content-Type、Authorization 等）
                    .AllowAnyHeader()

                    // 允许携带凭证（Cookie）
                    // V2 的 Refresh Token 存在 HttpOnly Cookie 里，需要这个
                    .AllowCredentials();
            });
        });

        return services;
    }
}