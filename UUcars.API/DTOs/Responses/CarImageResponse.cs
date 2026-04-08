namespace UUcars.API.DTOs.Responses;

public class CarImageResponse
{
    public int Id { get; set; }
    public string ImageUrl { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public int CarId { get; set; }
}