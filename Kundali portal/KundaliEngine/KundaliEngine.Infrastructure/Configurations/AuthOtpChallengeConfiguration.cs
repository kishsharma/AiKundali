using KundaliEngine.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KundaliEngine.Infrastructure.Configurations;

public class AuthOtpChallengeConfiguration : IEntityTypeConfiguration<AuthOtpChallenge>
{
    public void Configure(EntityTypeBuilder<AuthOtpChallenge> entity)
    {
        entity.ToTable("AuthOtpChallenges", "public");

        entity.HasKey(x => x.Id);

        entity.Property(x => x.Id)
            .HasDefaultValueSql("gen_random_uuid()");

        entity.Property(x => x.Purpose)
            .HasMaxLength(30)
            .IsRequired();

        entity.Property(x => x.Channel)
            .HasMaxLength(20)
            .HasDefaultValue("Email")
            .IsRequired();

        entity.Property(x => x.DestinationNormalized)
            .HasMaxLength(150)
            .IsRequired();

        entity.Property(x => x.CodeHash)
            .HasMaxLength(500)
            .IsRequired();

        entity.Property(x => x.RegistrationTokenHash)
            .HasMaxLength(500);

        entity.Property(x => x.IpAddress)
            .HasMaxLength(64);

        entity.Property(x => x.UserAgent)
            .HasMaxLength(500);

        entity.Property(x => x.CreatedAt)
            .HasDefaultValueSql("(now() AT TIME ZONE 'utc')");

        entity.HasIndex(x => new { x.Channel, x.Purpose, x.DestinationNormalized, x.CreatedAt });
        entity.HasIndex(x => x.ExpiresAt);
        entity.HasIndex(x => x.RegistrationTokenExpiresAt);

        entity.HasOne(x => x.User)
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
