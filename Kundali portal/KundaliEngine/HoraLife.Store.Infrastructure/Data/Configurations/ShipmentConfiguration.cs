using HoraLife.Store.Domain.Entities;
using HoraLife.Store.Domain.Enums;
using HoraLife.Store.Infrastructure.Data.Converters;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HoraLife.Store.Infrastructure.Data.Configurations;

internal sealed class ShipmentConfiguration : IEntityTypeConfiguration<Shipment>
{
    public void Configure(EntityTypeBuilder<Shipment> builder)
    {
        builder.ToTable("shipments", "store");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(x => x.OrderId).HasColumnName("order_id");
        builder.Property(x => x.CourierProvider).HasColumnName("courier_provider").HasMaxLength(100);
        builder.Property(x => x.TrackingNumber).HasColumnName("tracking_number").HasMaxLength(150);
        builder.Property(x => x.TrackingUrl).HasColumnName("tracking_url");

        builder.Property(x => x.ShipmentStatus)
            .HasColumnName("shipment_status")
            .HasMaxLength(30)
            .HasConversion(new SnakeCaseEnumConverter<ShipmentStatus>())
            .HasDefaultValue(ShipmentStatus.Pending);

        builder.Property(x => x.ShippedAt).HasColumnName("shipped_at");
        builder.Property(x => x.DeliveredAt).HasColumnName("delivered_at");
        builder.Property(x => x.EstimatedDelivery).HasColumnName("estimated_delivery").HasColumnType("date");
        builder.Property(x => x.ProviderResponse).HasColumnName("provider_response").HasColumnType("jsonb");
        builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("NOW()");
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("NOW()");

        builder.HasIndex(x => x.OrderId);
        builder.HasIndex(x => x.TrackingNumber);

        builder.HasOne(x => x.Order)
            .WithMany(x => x.Shipments)
            .HasForeignKey(x => x.OrderId)
            .OnDelete(DeleteBehavior.NoAction);
    }
}
