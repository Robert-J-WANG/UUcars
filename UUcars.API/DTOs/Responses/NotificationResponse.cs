namespace UUcars.API.DTOs.Responses;

public class NotificationResponse
{
    public int Id { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public int? RelatedId { get; set; }
    public bool IsRead { get; set; }
    public DateTime CreatedAt { get; set; }
}