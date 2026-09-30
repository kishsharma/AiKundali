using KundaliEngine.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KundaliEngine.Infrastructure.Configurations;

public class DhruvaQuestionLogConfiguration : IEntityTypeConfiguration<DhruvaQuestionLog>
{
    public void Configure(EntityTypeBuilder<DhruvaQuestionLog> entity)
    {
        entity.ToTable("DhruvaQuestionLogs", "public");
        entity.HasKey(x => x.Id);
        entity.HasIndex(x => x.UserId);
        entity.Property(x => x.Kind).HasMaxLength(40).IsRequired();
        entity.Property(x => x.Language).HasMaxLength(20).IsRequired();
        entity.Property(x => x.AskedAt).IsRequired();
    }
}
