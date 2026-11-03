using KundaliEngine.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KundaliEngine.Infrastructure.Configurations;

public sealed class PractitionerAppointmentConfiguration : IEntityTypeConfiguration<PractitionerAppointment>
{
    public void Configure(EntityTypeBuilder<PractitionerAppointment> entity)
    {
        entity.ToTable("PractitionerAppointments");
        entity.HasKey(x => x.Id);
        entity.Property(x => x.Id).HasDefaultValueSql("gen_random_uuid()");
        entity.Property(x => x.TimeZoneId).HasMaxLength(100).IsRequired();
        entity.Property(x => x.Status).HasMaxLength(24).IsRequired();
        entity.Property(x => x.Notes).HasMaxLength(1000);
        entity.Property(x => x.Topic).HasMaxLength(200);
        entity.HasIndex(x => new { x.UserId, x.Status, x.CompletedAtUtc });
        entity.HasIndex(x => new { x.UserId, x.StartsAtUtc });
        entity.HasIndex(x => new { x.Status, x.StartsAtUtc, x.ReminderSentVersion });
        entity.HasOne(x => x.Client).WithMany(x => x.Appointments).HasForeignKey(x => x.ClientId).OnDelete(DeleteBehavior.Cascade);
        entity.HasOne(x => x.Kundali).WithMany().HasForeignKey(x => x.KundaliId).OnDelete(DeleteBehavior.SetNull);
        entity.HasOne<User>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
    }
}
