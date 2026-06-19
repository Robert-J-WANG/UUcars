namespace UUcars.API.Entities.Audit;

/// <summary>
///     审计日志操作类型常量
///     用常量而不是枚举，直接存字符串到数据库，查询时更直观
/// </summary>
public static class AuditActions
{
    public const string CarApproved = "CarApproved";
    public const string CarRejected = "CarRejected";
    public const string CarDeleted = "CarDeleted";
}