using KundaliEngine.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KundaliEngine.Infrastructure.Configurations;

public class PortalNoteConfiguration : IEntityTypeConfiguration<PortalNote>
{
    public void Configure(EntityTypeBuilder<PortalNote> entity)
    {
        entity.ToTable("PortalNotes");

        entity.HasKey(x => x.Id);

        entity.Property(x => x.Id)
            .HasDefaultValueSql("gen_random_uuid()");

        entity.Property(x => x.ContextType)
            .HasMaxLength(50)
            .IsRequired();

        entity.Property(x => x.Content)
            .HasColumnType("text")
            .IsRequired();

        entity.Property(x => x.ContextTitle)
            .HasMaxLength(200);

        entity.Property(x => x.ContextDetailsJson)
            .HasColumnType("text");

        entity.Property(x => x.CreatedAt)
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        entity.Property(x => x.UpdatedAt)
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        entity.HasOne(x => x.User)
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.NoAction);

        entity.HasIndex(x => new { x.UserId, x.ContextType, x.ContextId })
            .IsUnique();
    }
}
