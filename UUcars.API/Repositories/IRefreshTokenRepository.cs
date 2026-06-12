using UUcars.API.Entities;

namespace UUcars.API.Repositories;

public interface IRefreshTokenRepository
{
    /// <summary>
    /// 根据 Token 字符串查找（验证时使用）
    /// </summary>
    Task<RefreshToken?> GetByTokenAsync(
        string token,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 保存新的 RefreshToken
    /// </summary>
    Task AddAsync(
        RefreshToken refreshToken,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 撤销单个 Token（登出 / Token 轮换时使用）
    /// </summary>
    Task RevokeAsync(
        RefreshToken refreshToken,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 撤销某个用户的所有 Token（强制全设备登出时使用）
    /// </summary>
    Task RevokeAllByUserIdAsync(
        int userId,
        CancellationToken cancellationToken = default);
}