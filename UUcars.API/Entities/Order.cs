using UUcars.API.Entities.Enums;

namespace UUcars.API.Entities;

public class Order : BaseEntity
{
    public decimal Price { get; set; }   // 下单时锁定的价格
    public OrderStatus Status { get; set; } = OrderStatus.Pending;

    // 外键 + 导航属性（车辆）
    public int CarId { get; set; }
    public Car Car { get; set; } = null!;

    // 外键 + 导航属性（买家）
    public int BuyerId { get; set; }
    public User Buyer { get; set; } = null!;

    // 外键 + 导航属性（卖家，从 Car 冗余存储）
    public int SellerId { get; set; }
    public User Seller { get; set; } = null!;
}