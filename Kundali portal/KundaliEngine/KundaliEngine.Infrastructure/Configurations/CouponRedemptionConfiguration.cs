using KundaliEngine.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KundaliEngine.Infrastructure.Configurations;

public class CouponRedemptionConfiguration : IEntityTypeConfiguration<CouponRedemption>
{
    public void Configure(EntityTypeBuilder<CouponRedemption> entity)
    {
        entity.ToTable("CouponRedemptions", "public");
        entity.HasKey(x => x.Id);
        entity.Property(x => x.RazorpayOrderId).HasMaxLength(80);
        entity.Property(x => x.PlanCode).HasMaxLength(50).IsRequired();
        entity.Property(x => x.BillingCycle).HasMaxLength(20).IsRequired();
        entity.HasIndex(x => x.RazorpayOrderId);
        entity.HasIndex(x => x.CouponOfferId);
    }
}
