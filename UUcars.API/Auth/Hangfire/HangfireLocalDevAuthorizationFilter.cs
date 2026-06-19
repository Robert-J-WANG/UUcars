using Hangfire.Dashboard;

namespace UUcars.API.Auth.Hangfire;

/// <summary>
/// 开发环境专用：直接放行，不做任何认证检查
/// 只在 IsDevelopment() 时使用，生产环境永远不用这个
/// </summary>
public class HangfireLocalDevAuthorizationFilter : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context)
    {
        return true;
    }
}