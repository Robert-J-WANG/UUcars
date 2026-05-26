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


        // ── 索引配置（V3 新增）───────────────────────────────────────

        // 索引 1：Status + CreatedAt
        // 用途：GET /cars 列表（最高频查询）
        //   WHERE Status = 'Published' ORDER BY CreatedAt DESC
        // Status 放前面（等值过滤），CreatedAt 放后面（排序）
        // IsDescending(false, true)：Status 升序，CreatedAt 降序
        builder.HasIndex(c => new { c.Status, c.CreatedAt })
            .HasDatabaseName("IX_Cars_Status_CreatedAt")
            .IsDescending(false, true);

        // 索引 2：Status + Brand
        // 用途：按品牌搜索
        //   WHERE Status = 'Published' AND Brand LIKE 'BMW%'
        // 注意：LIKE '%BMW%' 无法利用索引，但 LIKE 'BMW%'（前缀匹配）可以
        // 这个索引对前缀搜索有效，对包含搜索（Contains）无效
        // Step 65 会用全文索引彻底解决，现在先加着
        builder.HasIndex(c => new { c.Status, c.Brand })
            .HasDatabaseName("IX_Cars_Status_Brand");

        // 索引 3：Status + Price
        // 用途：价格区间过滤
        //   WHERE Status = 'Published' AND Price >= X AND Price <= Y
        builder.HasIndex(c => new { c.Status, c.Price })
            .HasDatabaseName("IX_Cars_Status_Price");

        // 索引 4：SellerId + Status + CreatedAt
        // 用途：卖家查自己的车辆列表
        //   WHERE SellerId = X AND Status != 'Deleted' ORDER BY CreatedAt DESC
        builder.HasIndex(c => new { c.SellerId, c.Status, c.CreatedAt })
            .HasDatabaseName("IX_Cars_SellerId_Status_CreatedAt")
            .IsDescending(false, false, true);
    }
}