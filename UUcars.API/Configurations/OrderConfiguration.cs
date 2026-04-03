using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using UUcars.API.Entities;

namespace UUcars.API.Configurations;

public class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.Property(o => o.Price)
            .IsRequired()
            .HasColumnType("decimal(18,2)");

        // 枚举存为字符串
        builder.Property(o => o.Status)
            .IsRequired()
            .HasMaxLength(20)
            .HasConversion<string>();

        builder.Property(o => o.CreatedAt)
            .IsRequired();

        builder.Property(o => o.UpdatedAt)
            .IsRequired();

        // 外键关系：订单关联车辆
        builder.HasOne(o => o.Car)
            .WithMany(c => c.Orders)
            .HasForeignKey(o => o.CarId)
            .OnDelete(DeleteBehavior.Restrict);

        // 外键关系：买家
        // 这里必须显式指定导航属性名，因为 User 有两个导航属性指向 Order
        // （BuyerOrders 和 SellerOrders），EF Core 无法自动判断该用哪个
        builder.HasOne(o => o.Buyer)
            .WithMany(u => u.BuyerOrders)
            .HasForeignKey(o => o.BuyerId)
            .OnDelete(DeleteBehavior.Restrict);

        // 外键关系：卖家
        builder.HasOne(o => o.Seller)
            .WithMany(u => u.SellerOrders)
            .HasForeignKey(o => o.SellerId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}