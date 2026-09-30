using KundaliEngine.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KundaliEngine.Infrastructure.Configurations;

public class UserSubscriptionConfiguration : IEntityTypeConfiguration<UserSubscription>
{
    public void Configure(EntityTypeBuilder<UserSubscription> entity)
    {
        entity.ToTable("UserSubscriptions", "public");

        entity.HasKey(x => x.Id);

        entity.Property(x => x.PlanCode)
            .HasMaxLength(50)
            .IsRequired();

        entity.Property(x => x.BillingCycle)
            .HasMaxLength(20)
            .IsRequired();

        entity.Property(x => x.Status)
            .HasMaxLength(40)
            .IsRequired();

        entity.Property(x => x.RazorpaySubscriptionId)
            .HasMaxLength(80);

        entity.Property(x => x.RazorpayOrderId)
            .HasMaxLength(80);

        entity.Property(x => x.RazorpayPaymentId)
            .HasMaxLength(80);

        entity.Property(x => x.CouponCode)
            .HasMaxLength(24);

        entity.HasIndex(x => x.UserId);
        entity.HasIndex(x => x.RazorpaySubscriptionId);
        entity.HasIndex(x => x.RazorpayOrderId);

        entity.HasOne(x => x.User)
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
