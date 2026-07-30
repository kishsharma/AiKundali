using HoraLife.Store.Domain.Entities;
using HoraLife.Store.Domain.Enums;
using HoraLife.Store.Infrastructure.Data.Converters;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HoraLife.Store.Infrastructure.Data.Configurations;

internal sealed class CartConfiguration : IEntityTypeConfiguration<Cart>
{
    public void Configure(EntityTypeBuilder<Cart> builder)
    {
        builder.ToTable("carts", "store");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(x => x.UserId).HasColumnName("user_id");
        builder.Property(x => x.GuestToken).HasColumnName("guest_token").HasMaxLength(150);

        builder.Property(x => x.Status)
            .HasColumnName("status")
            .HasMaxLength(20)
            .HasConversion(new SnakeCaseEnumConverter<CartStatus>())
            .HasDefaultValue(CartStatus.Active);

        builder.Property(x => x.Currency).HasColumnName("currency").HasColumnType("char(3)").HasDefaultValue("INR");
        builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("NOW()");
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("NOW()");

        builder.HasIndex(x => x.UserId)
            .IsUnique()
            .HasFilter("\"user_id\" IS NOT NULL AND \"status\" = 'active'")
            .HasDatabaseName("ux_active_user_cart");

        builder.HasIndex(x => x.GuestToken)
            .IsUnique()
            .HasFilter("\"guest_token\" IS NOT NULL AND \"status\" = 'active'")
            .HasDatabaseName("ux_active_guest_cart");
    }
}
