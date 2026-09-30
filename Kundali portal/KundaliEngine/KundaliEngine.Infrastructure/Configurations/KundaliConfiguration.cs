using KundaliEngine.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KundaliEngine.Infrastructure.Configurations;

public class KundaliConfiguration : IEntityTypeConfiguration<Kundali>
{
    public void Configure(EntityTypeBuilder<Kundali> entity)
    {
        entity.ToTable("Kundalis");

        entity.HasKey(x => x.Id);

        entity.Property(x => x.Id)
            .HasDefaultValueSql("gen_random_uuid()");

        entity.Property(x => x.FullName)
            .HasMaxLength(150)
            .IsRequired();

        entity.Property(x => x.Surname)
            .HasMaxLength(150);

        entity.Property(x => x.FatherName)
            .HasMaxLength(150);

        entity.Property(x => x.MotherName)
            .HasMaxLength(150);

        entity.Property(x => x.Gotra)
            .HasMaxLength(100);

        entity.Property(x => x.Gender)
            .HasMaxLength(20);

        entity.Property(x => x.DateOfBirth)
            .HasColumnType("date")
            .IsRequired();

        entity.Property(x => x.TimeOfBirth)
            .HasColumnType("time(6) without time zone")
            .IsRequired();

        entity.Property(x => x.BirthPlace)
            .HasMaxLength(200)
            .IsRequired();

        entity.Property(x => x.Latitude)
            .HasPrecision(9, 6);

        entity.Property(x => x.Longitude)
            .HasPrecision(9, 6);

        entity.Property(x => x.TimeZone)
            .HasMaxLength(50);

        entity.Property(x => x.ChartDataJson)
            .HasColumnType("text")
            .IsRequired();

        entity.Property(x => x.CreatedAt)
            .HasDefaultValueSql("(now() AT TIME ZONE 'utc')");

        entity.HasOne(x => x.User)
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.NoAction);
    }
}
