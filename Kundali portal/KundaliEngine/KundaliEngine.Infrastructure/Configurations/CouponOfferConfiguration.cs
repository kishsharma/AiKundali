using KundaliEngine.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KundaliEngine.Infrastructure.Configurations;

public class CouponOfferConfiguration : IEntityTypeConfiguration<CouponOffer>
{
    public void Configure(EntityTypeBuilder<CouponOffer> entity)
    {
        entity.ToTable("CouponOffers", "public");
        entity.HasKey(x => x.Id);
        entity.Property(x => x.Name).HasMaxLength(80).IsRequired();
        entity.Property(x => x.Code).HasMaxLength(24).IsRequired();
        entity.Property(x => x.PlanCodes).HasMaxLength(80).IsRequired();
        entity.Property(x => x.BillingCycles).HasMaxLength(40).IsRequired();
        entity.HasIndex(x => x.Code).IsUnique();
    }
}
