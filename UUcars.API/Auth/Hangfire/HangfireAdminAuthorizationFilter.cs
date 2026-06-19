using Hangfire.Dashboard;

namespace UUcars.API.Auth.Hangfire;

/// <summary>
///     Hangfire Dashboard 访问授权过滤器
///     只允许已认证的 Admin 角色用户访问 /hangfire 路由
/// </summary>
public class HangfireAdminAuthorizationFilter : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context)
    {
        var httpContext = context.GetHttpContext();

        // 必须已认证（登录）
        if (!httpContext.User.Identity?.IsAuthenticated ?? true)
            return false;

        // 必须是 Admin 角色
        return httpContext.User.IsInRole("Admin");
    }
}