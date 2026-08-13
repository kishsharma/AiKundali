using KundaliEngine.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KundaliEngine.Infrastructure.Configurations;

public class PortalNoteVoiceRecordingConfiguration : IEntityTypeConfiguration<PortalNoteVoiceRecording>
{
    public void Configure(EntityTypeBuilder<PortalNoteVoiceRecording> entity)
    {
        entity.ToTable("PortalNoteVoiceRecordings");

        entity.HasKey(x => x.Id);

        entity.Property(x => x.Id)
            .HasDefaultValueSql("gen_random_uuid()");

        entity.Property(x => x.ContextType)
            .HasMaxLength(50)
            .IsRequired();

        entity.Property(x => x.FileName)
            .HasMaxLength(255)
            .IsRequired();

        entity.Property(x => x.ContentType)
            .HasMaxLength(100)
            .IsRequired();

        entity.Property(x => x.StoragePath)
            .HasMaxLength(500)
            .IsRequired();

        entity.Property(x => x.CreatedAt)
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        entity.HasOne(x => x.User)
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.NoAction);

        entity.HasIndex(x => new { x.UserId, x.ContextType, x.ContextId });
        entity.HasIndex(x => x.CreatedAt);
    }
}
