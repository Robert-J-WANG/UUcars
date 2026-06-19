using System.ComponentModel.DataAnnotations;
using UUcars.API.Entities.Enums;

namespace UUcars.API.Entities;

public class Car : BaseEntity
{
    public string Title { get; set; } = string.Empty;
    public string Brand { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public int Year { get; set; }
    public decimal Price { get; set; }
    public int Mileage { get; set; }
    public string? Description { get; set; } // 允许为 null

    // 外键 + 导航属性
    public int SellerId { get; set; }
    public User Seller { get; set; } = null!;

    public CarStatus Status { get; set; } = CarStatus.Draft;

    // ✅ 新增：乐观锁并发令牌
    // SQL Server rowVersion 类型：每次行更新自动递增，不需要手动维护
    // EF Core 用它在 UPDATE 时加 WHERE RowVersion = @original 条件
    // 如果影响行数为 0，说明有人先改了这行 → 抛 DbUpdateConcurrencyException
    [Timestamp] public byte[] RowVersion { get; set; } = [];

    // 导航属性
    public ICollection<CarImage> Images { get; set; } = [];
    public ICollection<Order> Orders { get; set; } = [];
    public ICollection<Favorite> Favorites { get; set; } = [];
}