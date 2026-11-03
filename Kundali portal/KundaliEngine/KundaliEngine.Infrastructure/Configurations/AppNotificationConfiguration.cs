using KundaliEngine.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KundaliEngine.Infrastructure.Configurations;

public sealed class AppNotificationConfiguration : IEntityTypeConfiguration<AppNotification>
{
    public void Configure(EntityTypeBuilder<AppNotification> entity)
    {
        entity.ToTable("AppNotifications");
        entity.HasKey(x => x.Id);
        entity.Property(x => x.Id).HasDefaultValueSql("gen_random_uuid()");
        entity.Property(x => x.Type).HasMaxLength(40).IsRequired();
        entity.Property(x => x.Title).HasMaxLength(160).IsRequired();
        entity.Property(x => x.Message).HasMaxLength(500).IsRequired();
        entity.Property(x => x.Link).HasMaxLength(300).IsRequired();
        entity.Property(x => x.DedupeKey).HasMaxLength(180).IsRequired();
        entity.HasIndex(x => x.DedupeKey).IsUnique();
        entity.HasIndex(x => new { x.UserId, x.CreatedAt });
        entity.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
    }
}
