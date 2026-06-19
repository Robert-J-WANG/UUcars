namespace UUcars.API.Entities;

public class RefreshToken
{
    public int Id { get; set; }

    /// <summary>
    /// Token 本体：随机生成的不透明字符串（不是 JWT）
    /// 存数据库，每次使用后作废（Token 轮换）
    /// </summary>
    public string Token { get; set; } = string.Empty;

    /// <summary>
    /// 所属用户
    /// </summary>
    public int UserId { get; set; }

    public User User { get; set; } = null!;

    /// <summary>
    /// 过期时间：7天后
    /// </summary>
    public DateTime ExpiresAt { get; set; }

    /// <summary>
    /// 是否已被撤销
    /// true = 已使用过（Token 轮换）或用户主动登出
    /// </summary>
    public bool IsRevoked { get; set; } = false;

    public DateTime CreatedAt { get; set; }
}