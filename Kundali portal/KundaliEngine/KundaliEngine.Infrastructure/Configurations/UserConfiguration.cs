
using KundaliEngine.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KundaliEngine.Infrastructure.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> entity)
    {
        entity.ToTable("Users", "public");

        entity.HasKey(x => x.Id);

        entity.Property(x => x.FullName)
            .HasMaxLength(150)
            .IsRequired();

        entity.Property(x => x.Email)
            .HasMaxLength(150);

        entity.HasIndex(x => x.Email)
            .IsUnique()
            .HasFilter("\"Email\" IS NOT NULL");

        entity.Property(x => x.PhoneNumber)
            .HasMaxLength(20);

        entity.HasIndex(x => x.PhoneNumber)
            .IsUnique()
            .HasFilter("\"PhoneNumber\" IS NOT NULL");

        entity.Property(x => x.PasswordHash)
            .HasMaxLength(500);

        entity.Property(x => x.AuthProvider)
            .HasMaxLength(50)
            .HasDefaultValue("Local");

        entity.Property(x => x.Role)
            .HasMaxLength(50)
            .HasDefaultValue("User");

        entity.Property(x => x.IsActive)
            .HasDefaultValue(true);

        entity.Property(x => x.KundaliSaveLimit)
            .HasDefaultValue(5);

        entity.Property(x => x.CreatedAt)
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        entity.Property(x => x.EmailVerifiedAt);

        entity.Property(x => x.PhoneVerifiedAt);
    }
}


