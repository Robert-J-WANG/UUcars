namespace UUcars.API.Services.Hangfire;

using Microsoft.EntityFrameworkCore;
using Data;

/// <summary>
///     负责清理数据库里的过期 Token
///     由 Hangfire 每天凌晨定时调用
/// </summary>
public class TokenCleanupService
{
    private readonly AppDbContext _context;
    private readonly ILogger<TokenCleanupService> _logger;

    public TokenCleanupService(AppDbContext context, ILogger<TokenCleanupService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task CleanExpiredTokensAsync()
    {
        var now = DateTime.UtcNow;

        // 清理过期的密码重置 Token
        // 找出所有 ResetPasswordTokenExpiry 已过期的用户，清空他们的 Token 字段
        var expiredPasswordResets = await _context.Users
            .Where(u => u.ResetPasswordTokenExpiry != null
                        && u.ResetPasswordTokenExpiry < now)
            .ToListAsync();

        if (expiredPasswordResets.Count > 0)
        {
            foreach (var user in expiredPasswordResets)
            {
                user.ResetPasswordToken = null;
                user.ResetPasswordTokenExpiry = null;
                user.UpdatedAt = now;
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "Cleaned {Count} expired password reset tokens",
                expiredPasswordResets.Count);
        }
        else
        {
            _logger.LogInformation("No expired password reset tokens found");
        }
    }
}