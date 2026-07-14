using KundaliEngine.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KundaliEngine.Infrastructure.Configurations;

public class KundaliReportConfiguration : IEntityTypeConfiguration<KundaliReport>
{
    public void Configure(EntityTypeBuilder<KundaliReport> entity)
    {
        entity.ToTable("KundaliReports");

        entity.HasKey(x => x.Id);

        entity.Property(x => x.Id)
            .HasDefaultValueSql("gen_random_uuid()");

        entity.Property(x => x.ReportType)
            .HasMaxLength(50)
            .IsRequired();

        entity.Property(x => x.ReportStatus)
            .HasMaxLength(50)
            .HasDefaultValue("Generated")
            .IsRequired();

        entity.Property(x => x.FactsJson)
            .HasColumnType("jsonb")
            .IsRequired();

        entity.Property(x => x.ScoresJson)
            .HasColumnType("jsonb");

        entity.Property(x => x.SummaryJson)
            .HasColumnType("jsonb");

        entity.Property(x => x.ReportVersion)
            .HasMaxLength(20)
            .HasDefaultValue("v1")
            .IsRequired();

        entity.Property(x => x.CreatedAt)
            .HasDefaultValueSql("(now() AT TIME ZONE 'utc')");

        entity.HasOne(x => x.Kundali)
            .WithMany(x => x.Reports)
            .HasForeignKey(x => x.KundaliId)
            .OnDelete(DeleteBehavior.NoAction);

        entity.HasIndex(x => x.KundaliId)
            .HasDatabaseName("IX_KundaliReports_KundaliId");
    }
}
