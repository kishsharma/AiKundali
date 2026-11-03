using KundaliEngine.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KundaliEngine.Infrastructure.Configurations;

public sealed class PractitionerClientConfiguration : IEntityTypeConfiguration<PractitionerClient>
{
    public void Configure(EntityTypeBuilder<PractitionerClient> entity)
    {
        entity.ToTable("PractitionerClients");
        entity.HasKey(x => x.Id);
        entity.Property(x => x.Id).HasDefaultValueSql("gen_random_uuid()");
        entity.Property(x => x.FullName).HasMaxLength(150).IsRequired();
        entity.Property(x => x.Surname).HasMaxLength(150);
        entity.Property(x => x.FatherName).HasMaxLength(150);
        entity.Property(x => x.MotherName).HasMaxLength(150);
        entity.Property(x => x.Gotra).HasMaxLength(100);
        entity.Property(x => x.Gender).HasMaxLength(20).IsRequired();
        entity.Property(x => x.DateOfBirth).HasColumnType("date").IsRequired();
        entity.Property(x => x.TimeOfBirth).HasColumnType("time(6) without time zone").IsRequired();
        entity.Property(x => x.BirthPlace).HasMaxLength(200).IsRequired();
        entity.Property(x => x.Latitude).HasPrecision(9, 6);
        entity.Property(x => x.Longitude).HasPrecision(9, 6);
        entity.Property(x => x.PhoneNumber).HasMaxLength(30);
        entity.Property(x => x.Email).HasMaxLength(150);
        entity.Property(x => x.Topic).HasMaxLength(200);
        entity.Property(x => x.Status).HasMaxLength(32).IsRequired();
        entity.Property(x => x.Notes).HasMaxLength(2000);
        entity.HasIndex(x => new { x.UserId, x.IsArchived, x.FullName });
        entity.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
    }
}
