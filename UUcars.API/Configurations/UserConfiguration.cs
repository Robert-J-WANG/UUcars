using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using UUcars.API.Entities;

namespace UUcars.API.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.Property(u => u.Username)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(u => u.Email)
            .IsRequired()
            .HasMaxLength(100);

        // Email 唯一索引：同一个邮箱不能注册两次
        builder.HasIndex(u => u.Email)
            .IsUnique();

        builder.Property(u => u.EmailConfirmed)
            .IsRequired()
            .HasDefaultValue(false);

        // V2 新增：允许为 NULL（用户未发起验证时没有 Token）
        builder.Property(u => u.EmailConfirmationToken)
            .HasMaxLength(200);

        builder.Property(u => u.EmailConfirmationTokenExpiry);

        builder.Property(u => u.PasswordHash)
            .IsRequired()
            .HasMaxLength(256);

        // Step 39 新增
        builder.Property(u => u.ResetPasswordToken)
            .HasMaxLength(200);
        builder.Property(u => u.ResetPasswordTokenExpiry);

        // 枚举存为字符串：数据库里存 "User" / "Admin"，而不是 0 / 1
        // 这样直接看数据库也能看懂数据含义
        builder.Property(u => u.Role)
            .IsRequired()
            .HasMaxLength(20)
            .HasConversion<string>();

        builder.Property(u => u.EmailConfirmed)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(u => u.CreatedAt)
            .IsRequired();

        builder.Property(u => u.UpdatedAt)
            .IsRequired();
    }
}