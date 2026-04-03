namespace UUcars.API.Auth;

// 对应 appsettings.json 中的 "JwtSettings" 配置节
// 通过 Options 模式注入到需要的地方（Step 11 配置 JWT 认证时使用）
public class JwtSettings
{
    public string Secret { get; set; } = string.Empty;
    public int ExpiresInMinutes { get; set; } = 60;
    public string Issuer { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
}