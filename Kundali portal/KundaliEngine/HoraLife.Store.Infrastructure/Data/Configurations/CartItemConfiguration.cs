using HoraLife.Store.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HoraLife.Store.Infrastructure.Data.Configurations;

internal sealed class CartItemConfiguration : IEntityTypeConfiguration<CartItem>
{
    public void Configure(EntityTypeBuilder<CartItem> builder)
    {
        builder.ToTable("cart_items", "store");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(x => x.CartId).HasColumnName("cart_id");
        builder.Property(x => x.ProductId).HasColumnName("product_id");
        builder.Property(x => x.VariantId).HasColumnName("variant_id");
        builder.Property(x => x.Quantity).HasColumnName("quantity").HasDefaultValue(1);
        builder.Property(x => x.UnitPrice).HasColumnName("unit_price").HasPrecision(12, 2);
        builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("NOW()");
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("NOW()");

        builder.HasIndex(x => x.CartId);
        builder.HasIndex(x => x.ProductId);
        builder.HasIndex(x => x.VariantId);

        builder.HasOne(x => x.Cart)
            .WithMany(x => x.Items)
            .HasForeignKey(x => x.CartId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Product)
            .WithMany(x => x.CartItems)
            .HasForeignKey(x => x.ProductId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(x => x.Variant)
            .WithMany(x => x.CartItems)
            .HasForeignKey(x => x.VariantId)
            .OnDelete(DeleteBehavior.NoAction);
    }
}
