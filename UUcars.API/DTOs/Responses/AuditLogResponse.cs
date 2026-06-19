namespace UUcars.API.DTOs.Responses;

public class AuditLogResponse
{
    public int Id { get; set; }
    public int AdminId { get; set; }
    public string AdminUsername { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public int EntityId { get; set; }
    public string? Detail { get; set; }
    public DateTime CreatedAt { get; set; }
}