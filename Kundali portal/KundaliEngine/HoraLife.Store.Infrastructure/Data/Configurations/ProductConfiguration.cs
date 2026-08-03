using HoraLife.Store.Domain.Entities;
using HoraLife.Store.Domain.Enums;
using HoraLife.Store.Infrastructure.Data.Converters;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HoraLife.Store.Infrastructure.Data.Configurations;

internal sealed class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.ToTable("products", "store");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(x => x.CategoryId).HasColumnName("category_id");
        builder.Property(x => x.Sku).HasColumnName("sku").HasMaxLength(100).IsRequired();
        builder.Property(x => x.Slug).HasColumnName("slug").HasMaxLength(180).IsRequired();
        builder.Property(x => x.Name).HasColumnName("name").HasMaxLength(250).IsRequired();
        builder.Property(x => x.ShortDescription).HasColumnName("short_description").HasMaxLength(500);
        builder.Property(x => x.Description).HasColumnName("description");

        builder.Property(x => x.ProductType)
            .HasColumnName("product_type")
            .HasMaxLength(40)
            .HasConversion(new SnakeCaseEnumConverter<ProductType>())
            .IsRequired();

        builder.Property(x => x.BasePrice).HasColumnName("base_price").HasPrecision(12, 2);
        builder.Property(x => x.CompareAtPrice).HasColumnName("compare_at_price").HasPrecision(12, 2);
        builder.Property(x => x.Currency).HasColumnName("currency").HasColumnType("char(3)").HasDefaultValue("INR");
        builder.Property(x => x.Attributes).HasColumnName("attributes").HasColumnType("jsonb");
        builder.Property(x => x.RequiresConsultation).HasColumnName("requires_consultation").HasDefaultValue(false);
        builder.Property(x => x.IsFeatured).HasColumnName("is_featured").HasDefaultValue(false);
        builder.Property(x => x.IsActive).HasColumnName("is_active").HasDefaultValue(true);
        builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("NOW()");
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("NOW()");

        builder.HasIndex(x => x.Sku).IsUnique();
        builder.HasIndex(x => x.Slug).IsUnique();
        builder.HasIndex(x => new { x.CategoryId, x.IsActive });

        builder.HasOne(x => x.Category)
            .WithMany(x => x.Products)
            .HasForeignKey(x => x.CategoryId)
            .OnDelete(DeleteBehavior.NoAction);
    }
}
