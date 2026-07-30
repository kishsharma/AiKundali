using HoraLife.Store.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HoraLife.Store.Infrastructure.Persistence.Configurations;

internal sealed class RemedyKeyConfiguration : IEntityTypeConfiguration<RemedyKey>
{
    public void Configure(EntityTypeBuilder<RemedyKey> builder)
    {
        builder.ToTable("remedy_keys", "store");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(x => x.Key).HasColumnName("remedy_key").HasMaxLength(100).IsRequired();
        builder.Property(x => x.Name).HasColumnName("name").HasMaxLength(200).IsRequired();
        builder.Property(x => x.RemedyType).HasColumnName("remedy_type").HasMaxLength(40).IsRequired();
        builder.Property(x => x.Description).HasColumnName("description");
        builder.Property(x => x.IsActive).HasColumnName("is_active").HasDefaultValue(true);
        builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("NOW()");

        builder.HasIndex(x => x.Key).IsUnique();
    }
}
