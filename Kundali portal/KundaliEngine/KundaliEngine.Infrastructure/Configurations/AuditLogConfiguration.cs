using KundaliEngine.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KundaliEngine.Infrastructure.Configurations;

public class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> entity)
    {
        entity.ToTable("AuditLogs");

        entity.HasKey(x => x.Id);

        entity.Property(x => x.Action).HasMaxLength(100).IsRequired();
        entity.Property(x => x.Category).HasMaxLength(50).IsRequired();
        entity.Property(x => x.UserName).HasMaxLength(150);
        entity.Property(x => x.EntityType).HasMaxLength(100);
        entity.Property(x => x.EntityId).HasMaxLength(100);
        entity.Property(x => x.Description).HasMaxLength(1000);
        entity.Property(x => x.IpAddress).HasMaxLength(100);
        entity.Property(x => x.UserAgent).HasMaxLength(500);
        entity.Property(x => x.RequestPath).HasMaxLength(300);
        entity.Property(x => x.Status).HasMaxLength(30).HasDefaultValue("Success");
        entity.Property(x => x.FailureReason).HasMaxLength(1000);
        entity.Property(x => x.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");

        entity.HasIndex(x => x.CreatedAt);
        entity.HasIndex(x => x.UserId);
        entity.HasIndex(x => x.Action);
        entity.HasIndex(x => x.Category);
    }
}
