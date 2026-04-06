using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using UUcars.API.Entities;

namespace UUcars.API.Auth;

public class JwtTokenGenerator
{
    private readonly JwtSettings _jwtSettings;

    // IOptions<JwtSettings>：从 DI 容器中读取配置
    // Step 03 里已经在 Program.cs 用 Configure<JwtSettings> 绑定好了
    public JwtTokenGenerator(IOptions<JwtSettings> jwtSettings)
    {
        _jwtSettings = jwtSettings.Value;
    }

    public string GenerateToken(User user)
    {
        // =============================================
        // 第一步：定义 Claims（Token Payload 的内容）
        // =============================================
        // new Claim(key, value)：每一个 Claim 就是一个键值对
        //
        // JwtRegisteredClaimNames 是一个静态类，由`System.IdentityModel.Tokens.Jwt`包提供
        // 里面定义了所有 JWT 标准字段的名字
        // 比如 JwtRegisteredClaimNames.Sub 的值就是字符串 "sub"
        // 用这个类是为了避免手写字符串出错（写错了编译器不报错，运行时才发现）
        var claims = new[]
        {
            // sub：存用户 Id（转成字符串，因为 Claim 的 value 必须是字符串）
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),

            // email：存用户邮箱
            new Claim(JwtRegisteredClaimNames.Email, user.Email),

            // ClaimTypes.Role 是 ASP.NET Core 定义的角色 Claim Key
            // 它的实际值是一个很长的 URI：
            // "http://schemas.microsoft.com/ws/2008/06/identity/claims/role"
            // 必须用这个 Key（而不是随便写个 "role"），
            // 因为 [Authorize(Roles = "Admin")] 就是靠识别这个 Key 来判断角色的
            new Claim(ClaimTypes.Role, user.Role.ToString()),

            // jti（JWT ID）：给这个 Token 一个唯一标识
            // 用 Guid 生成，保证每个 Token 都不一样
            // 作用：如果以后要实现"注销 Token"功能，可以把 jti 存进黑名单
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        // =============================================
        // 第二步：创建签名凭证
        // =============================================
        // SymmetricSecurityKey：对称加密的 Key 对象
        // "对称"的意思是：签名和验证用同一个 Key（就是 appsettings 里的 JwtSettings:Secret）
        // Encoding.UTF8.GetBytes：把字符串 Secret 转成字节数组，Key 对象需要字节数组
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.Secret));

        // SigningCredentials：把 Key 和加密算法打包在一起
        // SecurityAlgorithms.HmacSha256 是 HMAC-SHA256 算法，目前最常用的 JWT 签名算法
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        // =============================================
        // 第三步：组装 Token
        // =============================================
        // JwtSecurityToken 是表示一个 JWT 的对象，把所有参数组装进去：
        var token = new JwtSecurityToken(
            issuer: _jwtSettings.Issuer,              // 签发方（谁签发的）
            audience: _jwtSettings.Audience,          // 受众（给谁用的）
            claims: claims,                           // Payload 内容
            expires: DateTime.UtcNow.AddMinutes(_jwtSettings.ExpiresInMinutes), // 过期时间
            signingCredentials: credentials           // 签名凭证
        );

        // =============================================
        // 第四步：序列化成字符串
        // =============================================
        // JwtSecurityTokenHandler 是处理 JWT 的工具类
        // WriteToken：把 JwtSecurityToken 对象序列化成 "xxxxx.yyyyy.zzzzz" 格式的字符串
        // 这个字符串就是最终返回给客户端的 Token
        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}