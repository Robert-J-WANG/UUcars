using Resend;
using UUcars.API.Configurations;

namespace UUcars.API.Extensions;

public static class EmailExtensions
{
    public static IServiceCollection AddEmailService(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // 绑定我们自己的 EmailSettings（FromEmail、FromName、BaseUrl 等）
        // 和 JwtSettings 的绑定方式完全一样
        services.Configure<EmailSettings>(
            configuration.GetSection("EmailSettings"));

        // 读取 API Key
        // 启动时就验证配置是否存在，而不是等到第一次发邮件才报错
        var apiKey = configuration["EmailSettings:ApiKey"]
                     ?? throw new InvalidOperationException(
                         "EmailSettings:ApiKey is not configured. " +
                         "Run: dotnet user-secrets set \"EmailSettings:ApiKey\" \"re_xxx\"");

        // 以下是 Resend 官方文档的标准注册方式
        services.AddOptions();
        services.AddHttpClient<ResendClient>();
        services.Configure<ResendClientOptions>(o => { o.ApiToken = apiKey; });
        services.AddTransient<IResend, ResendClient>();

        return services;
    }
}