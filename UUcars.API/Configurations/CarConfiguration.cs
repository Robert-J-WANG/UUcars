using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using UUcars.API.Entities;

namespace UUcars.API.Configurations;

public class CarConfiguration : IEntityTypeConfiguration<Car>
{
    public void Configure(EntityTypeBuilder<Car> builder)
    {
        builder.Property(c => c.Title)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(c => c.Brand)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(c => c.Model)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(c => c.Year)
            .IsRequired();

        // decimal 类型必须显式指定精度，约定不会自动处理
        // decimal(18,2)：最多 18 位数字，其中 2 位小数，适合存金额
        builder.Property(c => c.Price)
            .IsRequired()
            .HasColumnType("decimal(18,2)");

        builder.Property(c => c.Mileage)
            .IsRequired();

        // Description 没有 IsRequired()，对应数据库的 NULL（允许为空）
        builder.Property(c => c.Description)
            .HasMaxLength(2000);

        builder.Property(c => c.Status)
            .IsRequired()
            .HasMaxLength(20)
            .HasConversion<string>();

        builder.Property(c => c.CreatedAt)
            .IsRequired();

        builder.Property(c => c.UpdatedAt)
            .IsRequired();

        // 一辆车属于一个卖家（Car → User 是多对一）
        // Restrict：用户有车辆时不能被直接删除（安全防线）
        builder.HasOne(c => c.Seller)
            .WithMany(u => u.Cars)
            .HasForeignKey(c => c.SellerId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}