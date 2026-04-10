namespace UUcars.API.DTOs.Responses;

public class CarDetailResponse
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Brand { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public int Year { get; set; }
    public decimal Price { get; set; }
    public int Mileage { get; set; }
    public string? Description { get; set; }
    public string Status { get; set; } = string.Empty;
    public int SellerId { get; set; }
    public string SellerUsername { get; set; } = string.Empty;
    public List<CarImageResponse> Images { get; set; } = [];
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}