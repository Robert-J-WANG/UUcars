using UUcars.API.Services.Audit;

namespace UUcars.Tests.Fakes;

/// <summary>
///     测试用的审计日志服务：记录调用参数，不真正写数据库
///     和 FakeEmailService / FakeCacheService 保持一致的测试模式
/// </summary>
public class FakeAuditLogService : IAuditLogService
{
    public record LoggedEntry(
        int AdminId,
        string Action,
        string EntityType,
        int EntityId,
        string? Detail);

    public List<LoggedEntry> Entries { get; } = [];

    public Task LogAsync(
        int adminId,
        string action,
        string entityType,
        int entityId,
        string? detail = null,
        CancellationToken cancellationToken = default)
    {
        Entries.Add(new LoggedEntry(adminId, action, entityType, entityId, detail));
        return Task.CompletedTask;
    }
}