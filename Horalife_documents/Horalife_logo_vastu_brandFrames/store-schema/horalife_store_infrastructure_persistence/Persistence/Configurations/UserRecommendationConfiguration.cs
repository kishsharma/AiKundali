using HoraLife.Store.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HoraLife.Store.Infrastructure.Persistence.Configurations;

internal sealed class UserRecommendationConfiguration :
    IEntityTypeConfiguration<UserRecommendation>
{
    public void Configure(EntityTypeBuilder<UserRecommendation> builder)
    {
        builder.ToTable("user_recommendations", "store");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(x => x.UserId).HasColumnName("user_id");
        builder.Property(x => x.KundaliId).HasColumnName("kundali_id");
        builder.Property(x => x.RemedyKeyId).HasColumnName("remedy_key_id");
        builder.Property(x => x.ReasonCode).HasColumnName("reason_code").HasMaxLength(100);
        builder.Property(x => x.DisplayReason).HasColumnName("display_reason").HasMaxLength(500);
        builder.Property(x => x.ConfidenceScore).HasColumnName("confidence_score").HasPrecision(5, 2);
        builder.Property(x => x.ValidUntil).HasColumnName("valid_until");
        builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("NOW()");

        builder.HasIndex(x => new { x.UserId, x.KundaliId });
        builder.HasIndex(x => new { x.UserId, x.ValidUntil });

        builder.HasOne(x => x.RemedyKey)
            .WithMany(x => x.UserRecommendations)
            .HasForeignKey(x => x.RemedyKeyId)
            .OnDelete(DeleteBehavior.NoAction);
    }
}
