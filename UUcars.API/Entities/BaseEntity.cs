namespace UUcars.API.Entities;

// 所有实体的公共基类
// abstract：这个类本身不对应任何数据库表，
// 不应该被直接实例化，只能被继承
public abstract class BaseEntity
{
    public int Id { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}