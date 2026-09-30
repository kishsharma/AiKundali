using KundaliEngine.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KundaliEngine.Infrastructure.Configurations;

public class DhruvaDailyUsageConfiguration : IEntityTypeConfiguration<DhruvaDailyUsage>
{
    public void Configure(EntityTypeBuilder<DhruvaDailyUsage> entity)
    {
        entity.ToTable("DhruvaDailyUsages", "public");
        entity.HasKey(x => x.Id);
        entity.HasIndex(x => new { x.UserId, x.UsageDate }).IsUnique();
        entity.Property(x => x.LifeQuestionCount).IsRequired();
    }
}
