using KundaliEngine.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KundaliEngine.Infrastructure.Configurations;

public class AstrologerBrandingSettingConfiguration : IEntityTypeConfiguration<AstrologerBrandingSetting>
{
    public void Configure(EntityTypeBuilder<AstrologerBrandingSetting> entity)
    {
        entity.ToTable("AstrologerBrandingSettings");

        entity.HasKey(x => x.Id);

        entity.Property(x => x.AstrologerName)
            .HasMaxLength(150)
            .IsRequired();

        entity.Property(x => x.Designation)
            .HasMaxLength(120);

        entity.Property(x => x.Address)
            .HasMaxLength(500);

        entity.Property(x => x.ContactNumber)
            .HasMaxLength(30);

        entity.Property(x => x.Email)
            .HasMaxLength(150);

        entity.Property(x => x.LogoUrl)
            .HasMaxLength(500);

        entity.Property(x => x.ShowOnChartPage)
            .HasDefaultValue(true);

        entity.Property(x => x.ShowOnCompatibilityPage)
            .HasDefaultValue(true);

        entity.Property(x => x.ShowOnPdfReport)
            .HasDefaultValue(true);

        entity.Property(x => x.IsActive)
            .HasDefaultValue(true);

        entity.Property(x => x.CreatedAt)
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        entity.HasIndex(x => x.UserId)
            .IsUnique();

        entity.HasOne(x => x.User)
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
