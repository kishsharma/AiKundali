using HoraLife.Store.Domain.Entities;
using HoraLife.Store.Domain.Enums;
using HoraLife.Store.Infrastructure.Persistence.Converters;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HoraLife.Store.Infrastructure.Persistence.Configurations;

internal sealed class CustomerAddressConfiguration :
    IEntityTypeConfiguration<CustomerAddress>
{
    public void Configure(EntityTypeBuilder<CustomerAddress> builder)
    {
        builder.ToTable("customer_addresses", "store");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(x => x.UserId).HasColumnName("user_id");

        builder.Property(x => x.AddressType)
            .HasColumnName("address_type")
            .HasMaxLength(20)
            .HasConversion(new SnakeCaseEnumConverter<AddressType>())
            .HasDefaultValue(AddressType.Shipping);

        builder.Property(x => x.FullName).HasColumnName("full_name").HasMaxLength(200).IsRequired();
        builder.Property(x => x.Phone).HasColumnName("phone").HasMaxLength(30).IsRequired();
        builder.Property(x => x.AddressLine1).HasColumnName("address_line1").HasMaxLength(250).IsRequired();
        builder.Property(x => x.AddressLine2).HasColumnName("address_line2").HasMaxLength(250);
        builder.Property(x => x.City).HasColumnName("city").HasMaxLength(100).IsRequired();
        builder.Property(x => x.State).HasColumnName("state").HasMaxLength(100).IsRequired();
        builder.Property(x => x.PostalCode).HasColumnName("postal_code").HasMaxLength(20).IsRequired();
        builder.Property(x => x.CountryCode).HasColumnName("country_code").HasColumnType("char(2)").HasDefaultValue("IN");
        builder.Property(x => x.IsDefault).HasColumnName("is_default").HasDefaultValue(false);
        builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("NOW()");
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("NOW()");

        builder.HasIndex(x => x.UserId);
        builder.HasIndex(x => new { x.UserId, x.AddressType, x.IsDefault });
    }
}
