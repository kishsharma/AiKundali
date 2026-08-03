using HoraLife.Store.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HoraLife.Store.Infrastructure.Persistence.Configurations;

internal sealed class InventoryConfiguration : IEntityTypeConfiguration<Inventory>
{
    public void Configure(EntityTypeBuilder<Inventory> builder)
    {
        builder.ToTable("inventory", "store");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(x => x.ProductId).HasColumnName("product_id");
        builder.Property(x => x.VariantId).HasColumnName("variant_id");
        builder.Property(x => x.StockQuantity).HasColumnName("stock_quantity").HasDefaultValue(0);
        builder.Property(x => x.ReservedQuantity).HasColumnName("reserved_quantity").HasDefaultValue(0);
        builder.Property(x => x.ReorderLevel).HasColumnName("reorder_level").HasDefaultValue(0);
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("NOW()");
        builder.Ignore(x => x.AvailableQuantity);

        builder.HasIndex(x => x.ProductId);
        builder.HasIndex(x => x.VariantId);

        builder.HasOne(x => x.Product)
            .WithMany(x => x.InventoryRecords)
            .HasForeignKey(x => x.ProductId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Variant)
            .WithMany(x => x.InventoryRecords)
            .HasForeignKey(x => x.VariantId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
