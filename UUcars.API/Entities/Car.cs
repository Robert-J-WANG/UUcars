using UUcars.API.Entities.Enums;

namespace UUcars.API.Entities;

public class Car: BaseEntity
{
    public string Title { get; set; } = string.Empty;
    public string Brand { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public int Year { get; set; }
    public decimal Price { get; set; }
    public int Mileage { get; set; }
    public string? Description { get; set; }   // 允许为 null
    
    // 外键 + 导航属性
    public int SellerId { get; set; }
    public User Seller { get; set; } = null!;

    public CarStatus Status { get; set; } = CarStatus.Draft;

    // 导航属性
    public ICollection<CarImage> Images { get; set; } = [];
    public ICollection<Order> Orders { get; set; } = [];
    public ICollection<Favorite> Favorites { get; set; } = [];
}