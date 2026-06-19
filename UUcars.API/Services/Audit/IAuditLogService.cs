namespace UUcars.API.Services.Audit;

public interface IAuditLogService
{
    Task LogAsync(
        int adminId,
        string action,
        string entityType,
        int entityId,
        string? detail = null,
        CancellationToken cancellationToken = default);
}