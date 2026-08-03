using HoraLife.Store.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HoraLife.Store.Infrastructure.Persistence.Configurations;

internal sealed class ProductRemedyMappingConfiguration :
    IEntityTypeConfiguration<ProductRemedyMapping>
{
    public void Configure(EntityTypeBuilder<ProductRemedyMapping> builder)
    {
        builder.ToTable("product_remedy_mappings", "store");

        builder.HasKey(x => new { x.ProductId, x.RemedyKeyId });

        builder.Property(x => x.ProductId).HasColumnName("product_id");
        builder.Property(x => x.RemedyKeyId).HasColumnName("remedy_key_id");
        builder.Property(x => x.RecommendationRank).HasColumnName("recommendation_rank").HasDefaultValue(100);
        builder.Property(x => x.MatchStrength).HasColumnName("match_strength").HasPrecision(5, 2).HasDefaultValue(50m);
        builder.Property(x => x.Notes).HasColumnName("notes");

        builder.HasOne(x => x.Product)
            .WithMany(x => x.RemedyMappings)
            .HasForeignKey(x => x.ProductId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.RemedyKey)
            .WithMany(x => x.ProductMappings)
            .HasForeignKey(x => x.RemedyKeyId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
