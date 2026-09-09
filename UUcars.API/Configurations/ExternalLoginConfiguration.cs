using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using UUcars.API.Entities;

namespace UUcars.API.Configurations;

public class ExternalLoginConfiguration
    : IEntityTypeConfiguration<ExternalLogin>
{
    public void Configure(EntityTypeBuilder<ExternalLogin> builder)
    {
        builder.HasKey(login => login.Id);

        builder.Property(login => login.Provider)
            .IsRequired()
            .HasMaxLength(30);

        builder.Property(login => login.ProviderSubject)
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(login => login.CreatedAt)
            .IsRequired();

        builder.HasIndex(login => new
            {
                login.Provider,
                login.ProviderSubject
            })
            .IsUnique()
            .HasDatabaseName(
                "IX_ExternalLogins_Provider_ProviderSubject");

        builder.HasIndex(login => login.UserId)
            .HasDatabaseName("IX_ExternalLogins_UserId");

        builder.HasOne(login => login.User)
            .WithMany(user => user.ExternalLogins)
            .HasForeignKey(login => login.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
