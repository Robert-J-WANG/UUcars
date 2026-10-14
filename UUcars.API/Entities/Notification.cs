namespace UUcars.API.Entities;

/// <summary>
///     通知记录：实时推送 + 持久化，保证用户离线时也能看到历史通知
/// </summary>
public class Notification
{
    public int Id { get; set; }

    /// <summary>
    /// 接收者（目标用户）
    /// </summary>
    public int UserId { get; set; }

    // 关联到 User 表
    public User User { get; set; } = null!;

    /// <summary>
    /// 通知类型，用于前端决定跳转到哪个页面
    /// </summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// 通知正文，直接展示给用户
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// 关联的业务实体 Id（CarId 或 OrderId），用于跳转
    /// </summary>
    public int? RelatedId { get; set; }

    /// <summary>
    /// 是否已读，默认未读
    /// </summary>
    public bool IsRead { get; set; } = false;

    public DateTime CreatedAt { get; set; }
}