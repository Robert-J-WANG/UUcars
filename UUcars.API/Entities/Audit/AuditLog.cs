namespace UUcars.API.Entities;

/// <summary>
///     审计日志：记录 Admin 的关键操作
///     不继承 BaseEntity（不需要 UpdatedAt，审计日志只写入，不修改）
/// </summary>
public class AuditLog
{
    public int Id { get; set; }

    /// <summary>
    /// 操作者（Admin 用户）的 Id
    /// </summary>
    public int AdminId { get; set; }

    public User Admin { get; set; } = null!;

    /// <summary>
    /// 操作类型，例如："CarApproved" / "CarRejected" / "CarDeleted"
    /// </summary>
    public string Action { get; set; } = string.Empty;

    /// <summary>
    /// 被操作的实体类型，例如："Car"
    /// </summary>
    public string EntityType { get; set; } = string.Empty;

    /// <summary>
    /// 被操作的实体 Id
    /// </summary>
    public int EntityId { get; set; }

    /// <summary>
    /// 补充说明（可选）
    /// </summary>
    public string? Detail { get; set; }

    public DateTime CreatedAt { get; set; }
}