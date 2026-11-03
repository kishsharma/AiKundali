using KundaliEngine.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KundaliEngine.Infrastructure.Configurations;

public sealed class AuthRefreshSessionConfiguration : IEntityTypeConfiguration<AuthRefreshSession>
{
    public void Configure(EntityTypeBuilder<AuthRefreshSession> entity)
    {
        entity.ToTable("AuthRefreshSessions", "public");
        entity.HasKey(x => x.Id);
        entity.Property(x => x.TokenHash).HasMaxLength(64).IsRequired();
        entity.Property(x => x.ReplacedByTokenHash).HasMaxLength(64);
        entity.Property(x => x.RememberMe).IsRequired();
        entity.Property(x => x.CreatedAtUtc).IsRequired();
        entity.Property(x => x.ExpiresAtUtc).IsRequired();
        entity.HasIndex(x => x.TokenHash).IsUnique();
        entity.HasIndex(x => new { x.FamilyId, x.RevokedAtUtc });
        entity.HasIndex(x => new { x.UserId, x.RevokedAtUtc });
        entity.HasOne<User>()
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
