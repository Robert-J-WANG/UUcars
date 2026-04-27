namespace UUcars.API.DTOs.Responses;

public class ReviewResponse
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public int ReviewerId { get; set; }
    public string ReviewerUsername { get; set; } = string.Empty;
    public int Rating { get; set; }
    public string? Comment { get; set; }
    public DateTime CreatedAt { get; set; }
}