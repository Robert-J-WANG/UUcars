using System.Security.Claims;
namespace UUcars.API.Services;

// 封装"从当前请求的 JWT Token 里提取用户信息"的逻辑
public class CurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    // 需要通过 IHttpContextAccessor 间接访问当前 HTTP 请求的上下文。
    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public int? GetCurrentUserId()
    {
        var user = _httpContextAccessor.HttpContext?.User;
        if (user == null) return null;

        // ClaimTypes.NameIdentifier 是 ASP.NET Core 对 JWT "sub" Claim 的映射名
        // JWT 库在解析 Token 时，会把 "sub" 这个标准 Claim
        // 自动映射成 ClaimTypes.NameIdentifier（一个很长的 URI 字符串）
        var value = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        // Claim 的 value 是字符串，需要转成 int
        // int.TryParse：转换失败时返回 false，不会抛异常（比 int.Parse 更安全）
        if (int.TryParse(value, out var userId))
            return userId;

        return null;
    }
}