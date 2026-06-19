using UUcars.API.Data;
using UUcars.API.Entities;

namespace UUcars.API.Services.Audit;

public class AuditLogService : IAuditLogService
{
    private readonly AppDbContext _context;
    private readonly ILogger<AuditLogService> _logger;

    public AuditLogService(AppDbContext context, ILogger<AuditLogService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task LogAsync(
        int adminId,
        string action,
        string entityType,
        int entityId,
        string? detail = null,
        CancellationToken cancellationToken = default)
    {
        var auditLog = new AuditLog
        {
            AdminId = adminId,
            Action = action,
            EntityType = entityType,
            EntityId = entityId,
            Detail = detail,
            CreatedAt = DateTime.UtcNow
        };

        _context.AuditLogs.Add(auditLog);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Audit: Admin {AdminId} performed {Action} on {EntityType} {EntityId}",
            adminId, action, entityType, entityId);
    }
}