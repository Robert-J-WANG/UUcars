using System.Security.Claims;
using System.Threading.RateLimiting;

namespace UUcars.API.Extensions;

public static class RateLimitingExtensions
{
    public static IServiceCollection AddRateLimiting(this IServiceCollection services)
    {
        services.AddRateLimiter(options =>
        {
            // 被限流时统一返回 429，并附上标准 ApiResponse 格式的错误信息
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            // OnRejected：每次有请求被拒绝时执行的回调
            // 用途：
            //   1. 写入 Retry-After 响应头（告诉客户端多少秒后重试）
            //   2. 返回符合我们 ApiResponse 格式的 JSON，而不是空响应
            options.OnRejected = async (context, cancellationToken) =>
            {
                var httpContext = context.HttpContext;
                httpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                httpContext.Response.ContentType = "application/json";

                // 从限流器里取出"多少秒后可以重试"
                // 不是所有限流器都提供这个信息，所以用 TryGetMetadata 安全读取
                if (context.Lease.TryGetMetadata(
                        MetadataName.RetryAfter, out var retryAfter))
                    httpContext.Response.Headers.RetryAfter =
                        ((int)retryAfter.TotalSeconds).ToString();

                // 返回符合 ApiResponse 格式的 JSON
                var response = new
                {
                    success = false,
                    message = "Too many requests. Please slow down and try again later.",
                    errors = (object?)null
                };

                await httpContext.Response.WriteAsJsonAsync(
                    response, cancellationToken);
            };

            // ── 策略1： 登录接口 ─────────────────────────
            // 算法：固定窗口
            // 规则：同一 IP，每分钟最多 10 次
            // 原因：防止暴力破解密码
            //       10次/分钟 对正常用户完全够用（谁会一分钟内登录10次？）
            //       对暴力破解来说，10次/分钟意味着破解一个8位密码要几百万年
            options.AddPolicy(RateLimitPolicies.Login, httpContext =>
            {
                // 按 IP 分区：不同 IP 各自独立计数
                // 为什么不按用户 ID？登录接口还没有用户 ID（正在尝试登录）
                var ipAddress = httpContext.Connection.RemoteIpAddress?.ToString()
                                ?? "unknown";
                return RateLimitPartition.GetFixedWindowLimiter(
                    $"login:{ipAddress}",
                    _ => new FixedWindowRateLimiterOptions
                    {
                        Window = TimeSpan.FromMinutes(1),
                        PermitLimit = 10,
                        QueueLimit = 0, // 不排队，超出直接拒绝
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst
                    });
            });

            // ── 策略2：注册接口 ───────────────────────────
            // 算法：固定窗口
            // 规则：同一 IP，每小时最多 5 次
            // 原因：防止批量注册垃圾账号
            options.AddPolicy(RateLimitPolicies.Register, httpContext =>
            {
                var ipAddress = httpContext.Connection.RemoteIpAddress?.ToString()
                                ?? "unknown";
                return RateLimitPartition.GetFixedWindowLimiter(
                    $"register:{ipAddress}",
                    _ => new FixedWindowRateLimiterOptions
                    {
                        Window = TimeSpan.FromHours(1),
                        PermitLimit = 5,
                        QueueLimit = 0
                    });
            });

            // ── 策略3：忘记密码接口 ─────────────────────────
            // 算法：固定窗口
            // 规则：同一 IP，每小时最多 3 次
            // 原因：防止滥发密码重置邮件（有邮件发送成本，也是骚扰手段）
            options.AddPolicy(RateLimitPolicies.ForgotPassword, httpContext =>
            {
                var ipAddress = httpContext.Connection.RemoteIpAddress?.ToString()
                                ?? "unknown";
                return RateLimitPartition.GetFixedWindowLimiter(
                    $"forgot-password:{ipAddress}",
                    _ => new FixedWindowRateLimiterOptions
                    {
                        Window = TimeSpan.FromHours(1),
                        PermitLimit = 3,
                        QueueLimit = 0
                    });
            });

            // ── 策略4：重发验证邮件 ───────────────────────
            // 算法：固定窗口
            // 规则：同一 IP，每小时最多 3 次
            // 原因：它会触发发邮件，攻击者可以反复调用让系统滥发邮件
            options.AddPolicy(RateLimitPolicies.ResendVerification, httpContext =>
            {
                var ip = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
                return RateLimitPartition.GetFixedWindowLimiter(
                    $"resend-verification:{ip}",
                    _ => new FixedWindowRateLimiterOptions
                    {
                        Window = TimeSpan.FromHours(1),
                        PermitLimit = 3,
                        QueueLimit = 0
                    });
            });

            // ── 策略5：公开浏览接口 ──────────────────────────
            // 算法：令牌桶
            // 规则：
            //   已登录用户：按用户ID分区，每分钟 120 次（更宽松）
            //   未登录用户：按 IP 分区，每分钟 60 次
            // 原因：
            //   - 允许正常用户快速翻页（令牌桶支持短暂突发）
            //   - 已登录用户给更高配额（他们是真实注册用户，风险更低）
            //   - 公司/学校共用 IP 的情况下，按 IP 限制可能误伤，
            //     但未登录用户无法区分个体，只能按 IP
            options.AddPolicy(RateLimitPolicies.Browse, httpContext =>
            {
                // 检查是否已登录（JWT 认证中间件已经处理过了）
                var userId = httpContext.User?.FindFirst(
                    ClaimTypes.NameIdentifier)?.Value;

                if (!string.IsNullOrEmpty(userId))
                    // 已登录：按用户 ID 分区，配额更高
                    return RateLimitPartition.GetTokenBucketLimiter(
                        $"browse:user:{userId}",
                        _ => new TokenBucketRateLimiterOptions
                        {
                            TokenLimit = 120, // 桶容量：最多积累 120 个令牌
                            ReplenishmentPeriod = TimeSpan.FromMinutes(1), // 每分钟补充
                            TokensPerPeriod = 120, // 每分钟补充 120 个令牌
                            AutoReplenishment = true, // 自动补充
                            QueueLimit = 0
                        });

                // 未登录：按 IP 分区
                var ipAddress = httpContext.Connection.RemoteIpAddress?.ToString()
                                ?? "unknown";
                return RateLimitPartition.GetTokenBucketLimiter(
                    $"browse:ip:{ipAddress}",
                    _ => new TokenBucketRateLimiterOptions
                    {
                        TokenLimit = 60,
                        ReplenishmentPeriod = TimeSpan.FromMinutes(1),
                        TokensPerPeriod = 60,
                        AutoReplenishment = true,
                        QueueLimit = 0
                    });
            });

            // ── 策略6：写操作 ─────────────────────────
            // 算法：令牌桶
            // 规则：
            //   已登录用户每分钟30次
            //   写操作必须登录，所以按用户 ID 分区
            //   未登录的写请求会被 [Authorize] 拦截，走不到这里
            options.AddPolicy(RateLimitPolicies.Write, httpContext =>
            {
                var userId = httpContext.User?.FindFirst(
                                 ClaimTypes.NameIdentifier)?.Value
                             ?? httpContext.Connection.RemoteIpAddress?.ToString()
                             ?? "unknown";

                return RateLimitPartition.GetTokenBucketLimiter(
                    $"write:{userId}",
                    _ => new TokenBucketRateLimiterOptions
                    {
                        TokenLimit = 30,
                        ReplenishmentPeriod = TimeSpan.FromMinutes(1),
                        TokensPerPeriod = 30,
                        AutoReplenishment = true,
                        QueueLimit = 0
                    });
            });
        });
        return services;
    }
}