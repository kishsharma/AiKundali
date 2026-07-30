using HoraLife.Store.Domain.Entities;
using HoraLife.Store.Domain.Enums;
using HoraLife.Store.Infrastructure.Persistence.Converters;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HoraLife.Store.Infrastructure.Persistence.Configurations;

internal sealed class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.ToTable("orders", "store");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(x => x.OrderNumber)
            .HasColumnName("order_number")
            .UseIdentityAlwaysColumn()
            .ValueGeneratedOnAdd();

        builder.Property(x => x.UserId).HasColumnName("user_id");
        builder.Property(x => x.CartId).HasColumnName("cart_id");
        builder.Property(x => x.CustomerEmail).HasColumnName("customer_email").HasMaxLength(250).IsRequired();
        builder.Property(x => x.CustomerPhone).HasColumnName("customer_phone").HasMaxLength(30);

        builder.Property(x => x.OrderStatus)
            .HasColumnName("order_status")
            .HasMaxLength(30)
            .HasConversion(new SnakeCaseEnumConverter<OrderStatus>())
            .HasDefaultValue(OrderStatus.Pending);

        builder.Property(x => x.PaymentStatus)
            .HasColumnName("payment_status")
            .HasMaxLength(30)
            .HasConversion(new SnakeCaseEnumConverter<OrderPaymentStatus>())
            .HasDefaultValue(OrderPaymentStatus.Pending);

        builder.Property(x => x.SubtotalAmount).HasColumnName("subtotal_amount").HasPrecision(12, 2);
        builder.Property(x => x.DiscountAmount).HasColumnName("discount_amount").HasPrecision(12, 2);
        builder.Property(x => x.ShippingAmount).HasColumnName("shipping_amount").HasPrecision(12, 2);
        builder.Property(x => x.TaxAmount).HasColumnName("tax_amount").HasPrecision(12, 2);
        builder.Property(x => x.TotalAmount).HasColumnName("total_amount").HasPrecision(12, 2);
        builder.Property(x => x.Currency).HasColumnName("currency").HasColumnType("char(3)").HasDefaultValue("INR");
        builder.Property(x => x.ShippingAddress).HasColumnName("shipping_address").HasColumnType("jsonb").IsRequired();
        builder.Property(x => x.BillingAddress).HasColumnName("billing_address").HasColumnType("jsonb");

        builder.Property(x => x.OrderSource)
            .HasColumnName("order_source")
            .HasMaxLength(40)
            .HasConversion(new SnakeCaseEnumConverter<OrderSource>())
            .HasDefaultValue(OrderSource.DirectStore);

        builder.Property(x => x.RecommendedKundaliId).HasColumnName("recommended_kundali_id");
        builder.Property(x => x.Notes).HasColumnName("notes");
        builder.Property(x => x.PlacedAt).HasColumnName("placed_at").HasDefaultValueSql("NOW()");
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("NOW()");

        builder.HasIndex(x => new { x.UserId, x.PlacedAt });
        builder.HasIndex(x => new { x.OrderStatus, x.PlacedAt });

        builder.HasOne(x => x.Cart)
            .WithMany(x => x.Orders)
            .HasForeignKey(x => x.CartId)
            .OnDelete(DeleteBehavior.NoAction);
    }
}
