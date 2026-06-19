using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using UUcars.API.Entities;

namespace UUcars.API.Configurations;

public class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.HasKey(a => a.Id);

        builder.Property(a => a.Action)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(a => a.EntityType)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(a => a.Detail)
            .HasMaxLength(500);

        builder.Property(a => a.CreatedAt)
            .IsRequired();

        // 关联 Admin 用户
        // Restrict：Admin 用户不能被删除（如果还有审计记录关联着他）
        // 保护审计记录的完整性，不让删除用户连带删除历史操作记录
        builder.HasOne(a => a.Admin)
            .WithMany()
            .HasForeignKey(a => a.AdminId)
            .OnDelete(DeleteBehavior.Restrict);

        // 常见查询场景：按操作类型筛选，按时间排序
        builder.HasIndex(a => new { a.Action, a.CreatedAt })
            .HasDatabaseName("IX_AuditLogs_Action_CreatedAt");
    }
}