using KundaliEngine.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KundaliEngine.Infrastructure.Configurations;

public class DhruvaChatMessageConfiguration : IEntityTypeConfiguration<DhruvaChatMessage>
{
    public void Configure(EntityTypeBuilder<DhruvaChatMessage> entity)
    {
        entity.ToTable("DhruvaChatMessages", "public");
        entity.HasKey(x => x.Id);
        entity.Property(x => x.Question).HasMaxLength(2000).IsRequired();
        entity.Property(x => x.Response).IsRequired();
        entity.Property(x => x.AskedAt).IsRequired();
        entity.HasIndex(x => new { x.UserId, x.KundaliId, x.AskedAt });
    }
}
