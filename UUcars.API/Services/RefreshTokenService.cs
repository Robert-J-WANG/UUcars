using UUcars.API.Auth;
using UUcars.API.Data;
using UUcars.API.Entities;
using UUcars.API.Exceptions;
using UUcars.API.Repositories;

namespace UUcars.API.Services;

public class RefreshTokenService
{
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly JwtTokenGenerator _jwtTokenGenerator;
    private readonly AppDbContext _context;
    private readonly ILogger<RefreshTokenService> _logger;

    // RefreshToken 有效期：7天
    private const int RefreshTokenExpiryDays = 7;

    public RefreshTokenService(
        IRefreshTokenRepository refreshTokenRepository,
        JwtTokenGenerator jwtTokenGenerator,
        AppDbContext context,
        ILogger<RefreshTokenService> logger)
    {
        _refreshTokenRepository = refreshTokenRepository;
        _jwtTokenGenerator = jwtTokenGenerator;
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// 生成新的 RefreshToken 并存入数据库
    /// 在登录成功后调用
    /// </summary>
    public async Task<RefreshToken> CreateRefreshTokenAsync(
        int userId,
        CancellationToken cancellationToken = default)
    {
        var refreshToken = new RefreshToken
        {
            // 使用 cryptographically secure 随机字节生成 Token
            // 不用 Guid：Guid 的随机性不够强（Version 4 只有 122 位随机）
            // 64 字节的随机数 = 512 位熵，远超攻击者暴力破解的可能
            Token = TokenGenerator.Generate(),
            UserId = userId,
            ExpiresAt = DateTime.UtcNow.AddDays(RefreshTokenExpiryDays),
            IsRevoked = false,
            CreatedAt = DateTime.UtcNow
        };

        await _refreshTokenRepository.AddAsync(refreshToken, cancellationToken);
        return refreshToken;
    }

    /// <summary>
    /// 验证 RefreshToken，签发新的 AccessToken + RefreshToken（Token 轮换）
    /// </summary>
    public async Task<(string NewAccessToken, RefreshToken NewRefreshToken)> RefreshAsync(
        string token,
        CancellationToken cancellationToken = default)
    {
        // 1. 根据 Token 字符串查找记录
        var existingToken = await _refreshTokenRepository
            .GetByTokenAsync(token, cancellationToken);

        // 2. Token 不存在
        if (existingToken == null)
        {
            _logger.LogWarning("Refresh attempt with unknown token");
            throw new InvalidTokenException();
        }

        // 3. Token 已被撤销
        // ⚠️ 重要安全检查：如果已撤销的 Token 被使用，说明可能发生了 Token 盗用
        if (existingToken.IsRevoked)
        {
            _logger.LogWarning(
                "Revoked refresh token used for user {UserId}. Possible token theft detected.",
                existingToken.UserId);

            // 安全措施：撤销该用户的所有 Token，强制重新登录
            // 这是防止 Token 盗用的关键步骤
            await _refreshTokenRepository.RevokeAllByUserIdAsync(
                existingToken.UserId, cancellationToken);

            throw new InvalidTokenException();
        }

        // 4. Token 已过期
        if (existingToken.ExpiresAt < DateTime.UtcNow)
        {
            _logger.LogInformation(
                "Expired refresh token used for user {UserId}",
                existingToken.UserId);
            throw new InvalidTokenException();
        }

        var user = existingToken.User;

        // 5. Token 轮换：撤销旧 Token
        await _refreshTokenRepository.RevokeAsync(existingToken, cancellationToken);

        // 6. 签发新 AccessToken
        var newAccessToken = _jwtTokenGenerator.GenerateToken(user);

        // 7. 生成新 RefreshToken
        var newRefreshToken = new RefreshToken
        {
            Token = TokenGenerator.Generate(),
            UserId = user.Id,
            ExpiresAt = DateTime.UtcNow.AddDays(RefreshTokenExpiryDays),
            IsRevoked = false,
            CreatedAt = DateTime.UtcNow
        };

        await _refreshTokenRepository.AddAsync(newRefreshToken, cancellationToken);

        // 8. 一次性保存（旧 Token 撤销 + 新 Token 创建，保证原子性）
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Refresh token rotated for user {UserId}", user.Id);

        return (newAccessToken, newRefreshToken);
    }

    /// <summary>
    /// 撤销当前用户的 RefreshToken（登出）
    /// </summary>
    public async Task RevokeAsync(
        string token,
        CancellationToken cancellationToken = default)
    {
        var existingToken = await _refreshTokenRepository
            .GetByTokenAsync(token, cancellationToken);

        // Token 不存在或已撤销：静默处理，不报错
        // 原因：前端可能重复调登出接口，或 Token 已过期被清理，都应该返回成功
        if (existingToken == null || existingToken.IsRevoked)
        {
            _logger.LogInformation("Logout: token not found or already revoked");
            return;
        }

        await _refreshTokenRepository.RevokeAsync(existingToken, cancellationToken);

        _logger.LogInformation(
            "Refresh token revoked for user {UserId}", existingToken.UserId);
    }
}