namespace UUcars.API.Entities;

// Favorite 不继承 BaseEntity
// 因为它用 (UserId, CarId) 联合主键，没有独立的 Id 字段
// 联合主键需要在 DbContext 里用 Fluent API 配置，
// 仅靠 Data Annotations 无法完整表达
public class Favorite
{
    public DateTime CreatedAt { get; set; }
    
    // 外键 + 导航属性
    public int UserId { get; set; }
    public User User { get; set; } = null!;

    // 外键 + 导航属性
    public int CarId { get; set; }
    public Car Car { get; set; } = null!;

    
}