using KundaliEngine.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KundaliEngine.Infrastructure.Configurations;

public class CommunicationLogConfiguration : IEntityTypeConfiguration<CommunicationLog>
{
    public void Configure(EntityTypeBuilder<CommunicationLog> entity)
    {
        entity.ToTable("CommunicationLogs");

        entity.HasKey(x => x.Id);

        entity.Property(x => x.Channel).HasMaxLength(30).IsRequired();
        entity.Property(x => x.RecipientEmail).HasMaxLength(150);
        entity.Property(x => x.RecipientMobile).HasMaxLength(30);
        entity.Property(x => x.MessageType).HasMaxLength(50).IsRequired();
        entity.Property(x => x.TemplateName).HasMaxLength(100);
        entity.Property(x => x.Subject).HasMaxLength(200);
        entity.Property(x => x.Status).HasMaxLength(30).HasDefaultValue("Sent");
        entity.Property(x => x.Provider).HasMaxLength(100);
        entity.Property(x => x.ProviderMessageId).HasMaxLength(150);
        entity.Property(x => x.FailureReason).HasMaxLength(1000);
        entity.Property(x => x.RelatedEntityType).HasMaxLength(100);
        entity.Property(x => x.RelatedEntityId).HasMaxLength(100);
        entity.Property(x => x.TriggeredByUserName).HasMaxLength(150);
        entity.Property(x => x.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");

        entity.HasIndex(x => x.CreatedAt);
        entity.HasIndex(x => x.Channel);
        entity.HasIndex(x => x.Status);
        entity.HasIndex(x => x.RecipientEmail);
        entity.HasIndex(x => x.RecipientMobile);
    }
}
