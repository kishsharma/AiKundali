using HoraLife.Store.Domain.Entities;
using HoraLife.Store.Domain.Enums;
using HoraLife.Store.Infrastructure.Persistence.Converters;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HoraLife.Store.Infrastructure.Persistence.Configurations;

internal sealed class PaymentConfiguration : IEntityTypeConfiguration<Payment>
{
    public void Configure(EntityTypeBuilder<Payment> builder)
    {
        builder.ToTable("payments", "store");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(x => x.OrderId).HasColumnName("order_id");
        builder.Property(x => x.Provider).HasColumnName("provider").HasMaxLength(50).IsRequired();
        builder.Property(x => x.ProviderPaymentId).HasColumnName("provider_payment_id").HasMaxLength(200);
        builder.Property(x => x.ProviderOrderId).HasColumnName("provider_order_id").HasMaxLength(200);
        builder.Property(x => x.ProviderEventId).HasColumnName("provider_event_id").HasMaxLength(200);

        builder.Property(x => x.PaymentStatus)
            .HasColumnName("payment_status")
            .HasMaxLength(30)
            .HasConversion(new SnakeCaseEnumConverter<PaymentStatus>())
            .HasDefaultValue(PaymentStatus.Pending);

        builder.Property(x => x.Amount).HasColumnName("amount").HasPrecision(12, 2);
        builder.Property(x => x.Currency).HasColumnName("currency").HasColumnType("char(3)").HasDefaultValue("INR");
        builder.Property(x => x.PaymentMethod).HasColumnName("payment_method").HasMaxLength(50);
        builder.Property(x => x.GatewayResponse).HasColumnName("gateway_response").HasColumnType("jsonb");
        builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("NOW()");
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("NOW()");

        builder.HasIndex(x => x.OrderId);

        builder.HasIndex(x => new { x.Provider, x.ProviderEventId })
            .IsUnique()
            .HasFilter("\"provider_event_id\" IS NOT NULL")
            .HasDatabaseName("ux_payment_provider_event");

        builder.HasOne(x => x.Order)
            .WithMany(x => x.Payments)
            .HasForeignKey(x => x.OrderId)
            .OnDelete(DeleteBehavior.NoAction);
    }
}
