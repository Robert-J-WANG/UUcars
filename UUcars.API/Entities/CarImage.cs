namespace UUcars.API.Entities;

// CarImage 不继承 BaseEntity
// 因为它没有 CreatedAt / UpdatedAt，只有自己的 Id
public class CarImage
{
    public int Id { get; set; }
    public string ImageUrl { get; set; } = string.Empty;
    public int SortOrder { get; set; } = 0;

    // 外键 + 导航属性
    public int CarId { get; set; }
    public Car Car { get; set; } = null!;
}