using HoraLife.Store.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HoraLife.Store.Infrastructure.Persistence.Configurations;

internal sealed class OrderItemConfiguration : IEntityTypeConfiguration<OrderItem>
{
    public void Configure(EntityTypeBuilder<OrderItem> builder)
    {
        builder.ToTable("order_items", "store");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(x => x.OrderId).HasColumnName("order_id");
        builder.Property(x => x.ProductId).HasColumnName("product_id");
        builder.Property(x => x.VariantId).HasColumnName("variant_id");
        builder.Property(x => x.ProductName).HasColumnName("product_name").HasMaxLength(250).IsRequired();
        builder.Property(x => x.Sku).HasColumnName("sku").HasMaxLength(100).IsRequired();
        builder.Property(x => x.VariantName).HasColumnName("variant_name").HasMaxLength(200);
        builder.Property(x => x.Quantity).HasColumnName("quantity");
        builder.Property(x => x.UnitPrice).HasColumnName("unit_price").HasPrecision(12, 2);
        builder.Property(x => x.DiscountAmount).HasColumnName("discount_amount").HasPrecision(12, 2);
        builder.Property(x => x.TaxAmount).HasColumnName("tax_amount").HasPrecision(12, 2);
        builder.Property(x => x.LineTotal).HasColumnName("line_total").HasPrecision(12, 2);
        builder.Property(x => x.RecommendationId).HasColumnName("recommendation_id");

        builder.HasIndex(x => x.OrderId);

        builder.HasOne(x => x.Order)
            .WithMany(x => x.Items)
            .HasForeignKey(x => x.OrderId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Product)
            .WithMany(x => x.OrderItems)
            .HasForeignKey(x => x.ProductId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(x => x.Variant)
            .WithMany(x => x.OrderItems)
            .HasForeignKey(x => x.VariantId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(x => x.Recommendation)
            .WithMany(x => x.OrderItems)
            .HasForeignKey(x => x.RecommendationId)
            .OnDelete(DeleteBehavior.NoAction);
    }
}
