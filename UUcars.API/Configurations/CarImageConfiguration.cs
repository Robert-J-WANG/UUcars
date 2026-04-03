using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using UUcars.API.Entities;

namespace UUcars.API.Configurations;

public class CarImageConfiguration : IEntityTypeConfiguration<CarImage>
{
    public void Configure(EntityTypeBuilder<CarImage> builder)
    {
        builder.Property(ci => ci.ImageUrl)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(ci => ci.SortOrder)
            .IsRequired()
            .HasDefaultValue(0);

        // Cascade：车辆删除时图片跟着删，图片依附于车辆没有独立意义
        builder.HasOne(ci => ci.Car)
            .WithMany(c => c.Images)
            .HasForeignKey(ci => ci.CarId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}