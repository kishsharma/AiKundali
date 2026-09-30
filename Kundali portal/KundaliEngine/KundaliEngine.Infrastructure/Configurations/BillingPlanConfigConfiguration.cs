using KundaliEngine.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KundaliEngine.Infrastructure.Configurations;

public class BillingPlanConfigConfiguration : IEntityTypeConfiguration<BillingPlanConfig>
{
    public void Configure(EntityTypeBuilder<BillingPlanConfig> entity)
    {
        entity.ToTable("BillingPlanConfigs", "public");
        entity.HasKey(x => x.Code);
        entity.Property(x => x.Code).HasMaxLength(50);
        entity.Property(x => x.Name).HasMaxLength(80).IsRequired();
        entity.Property(x => x.Tagline).HasMaxLength(160).IsRequired();
        entity.Property(x => x.RazorpayMonthlyPlanId).HasMaxLength(80);
        entity.Property(x => x.RazorpayYearlyPlanId).HasMaxLength(80);
    }
}
