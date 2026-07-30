using KundaliEngine.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KundaliEngine.Infrastructure.Configurations;

public class SavedCompatibilityConfiguration : IEntityTypeConfiguration<SavedCompatibility>
{
    public void Configure(EntityTypeBuilder<SavedCompatibility> entity)
    {
        entity.ToTable("SavedCompatibilities");

        entity.HasKey(x => x.Id);

        entity.Property(x => x.Id)
            .HasDefaultValueSql("gen_random_uuid()");

        entity.Property(x => x.PersonAName)
            .HasMaxLength(150)
            .IsRequired();

        entity.Property(x => x.PersonAGender)
            .HasMaxLength(20);

        entity.Property(x => x.PersonADateOfBirth)
            .HasColumnType("date")
            .IsRequired();

        entity.Property(x => x.PersonATimeOfBirth)
            .HasColumnType("time(6) without time zone")
            .IsRequired();

        entity.Property(x => x.PersonABirthPlace)
            .HasMaxLength(200)
            .IsRequired();

        entity.Property(x => x.PersonALatitude)
            .HasPrecision(9, 6);

        entity.Property(x => x.PersonALongitude)
            .HasPrecision(9, 6);

        entity.Property(x => x.PersonBName)
            .HasMaxLength(150)
            .IsRequired();

        entity.Property(x => x.PersonBGender)
            .HasMaxLength(20);

        entity.Property(x => x.PersonBDateOfBirth)
            .HasColumnType("date")
            .IsRequired();

        entity.Property(x => x.PersonBTimeOfBirth)
            .HasColumnType("time(6) without time zone")
            .IsRequired();

        entity.Property(x => x.PersonBBirthPlace)
            .HasMaxLength(200)
            .IsRequired();

        entity.Property(x => x.PersonBLatitude)
            .HasPrecision(9, 6);

        entity.Property(x => x.PersonBLongitude)
            .HasPrecision(9, 6);

        entity.Property(x => x.AshtakootaScore)
            .HasPrecision(5, 2);

        entity.Property(x => x.RequestJson)
            .HasColumnType("text")
            .IsRequired();

        entity.Property(x => x.ResultJson)
            .HasColumnType("text")
            .IsRequired();

        entity.Property(x => x.CreatedAt)
            .HasDefaultValueSql("(now() AT TIME ZONE 'utc')");

        entity.HasIndex(x => new { x.UserId, x.CreatedAt });

        entity.HasOne(x => x.User)
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}