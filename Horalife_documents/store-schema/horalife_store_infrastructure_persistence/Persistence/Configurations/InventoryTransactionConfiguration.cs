using HoraLife.Store.Domain.Entities;
using HoraLife.Store.Domain.Enums;
using HoraLife.Store.Infrastructure.Persistence.Converters;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HoraLife.Store.Infrastructure.Persistence.Configurations;

internal sealed class InventoryTransactionConfiguration :
    IEntityTypeConfiguration<InventoryTransaction>
{
    public void Configure(EntityTypeBuilder<InventoryTransaction> builder)
    {
        builder.ToTable("inventory_transactions", "store");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(x => x.InventoryId).HasColumnName("inventory_id");

        builder.Property(x => x.TransactionType)
            .HasColumnName("transaction_type")
            .HasMaxLength(30)
            .HasConversion(new SnakeCaseEnumConverter<InventoryTransactionType>())
            .IsRequired();

        builder.Property(x => x.QuantityChange).HasColumnName("quantity_change");
        builder.Property(x => x.ReferenceType).HasColumnName("reference_type").HasMaxLength(50);
        builder.Property(x => x.ReferenceId).HasColumnName("reference_id");
        builder.Property(x => x.Notes).HasColumnName("notes");
        builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("NOW()");

        builder.HasIndex(x => new { x.InventoryId, x.CreatedAt });
        builder.HasIndex(x => new { x.ReferenceType, x.ReferenceId });

        builder.HasOne(x => x.Inventory)
            .WithMany(x => x.Transactions)
            .HasForeignKey(x => x.InventoryId)
            .OnDelete(DeleteBehavior.NoAction);
    }
}
