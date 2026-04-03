using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using UUcars.API.Entities;

namespace UUcars.API.Configurations;

public class FavoriteConfiguration : IEntityTypeConfiguration<Favorite>
{
    public void Configure(EntityTypeBuilder<Favorite> builder)
    {
        // 配置联合主键
        // new { f.UserId, f.CarId } 是 C# 的匿名对象写法，
        // 用来同时传入两个字段，告诉 EF Core"这两个字段一起构成主键"
        // Data Annotations 的 [Key] 只能标记单个字段，无法表达复合主键，
        // 这是必须用 Fluent API 的场景
        builder.HasKey(f => new { f.UserId, f.CarId });

        builder.Property(f => f.CreatedAt)
            .IsRequired();

        // 外键关系：收藏关联用户
        builder.HasOne(f => f.User)
            .WithMany(u => u.Favorites)
            .HasForeignKey(f => f.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // 外键关系：收藏关联车辆
        builder.HasOne(f => f.Car)
            .WithMany(c => c.Favorites)
            .HasForeignKey(f => f.CarId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}