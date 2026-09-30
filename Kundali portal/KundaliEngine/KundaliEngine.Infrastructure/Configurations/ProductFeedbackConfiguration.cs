using KundaliEngine.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KundaliEngine.Infrastructure.Configurations;

public class ProductFeedbackConfiguration : IEntityTypeConfiguration<ProductFeedback>
{
    public void Configure(EntityTypeBuilder<ProductFeedback> entity)
    {
        entity.ToTable("ProductFeedback", "public");
        entity.HasKey(x => x.Id);
        entity.Property(x => x.Surface).HasMaxLength(40).IsRequired();
        entity.Property(x => x.ContextKey).HasMaxLength(200).IsRequired();
        entity.Property(x => x.Sentiment).HasMaxLength(20).IsRequired();
        entity.Property(x => x.Topic).HasMaxLength(40);
        entity.Property(x => x.Note).HasMaxLength(400);
        entity.Property(x => x.PlanCode).HasMaxLength(50).IsRequired();
        entity.HasIndex(x => new { x.UserId, x.Surface, x.ContextKey, x.FeedbackDate }).IsUnique();
        entity.HasIndex(x => x.CreatedAt);
    }
}
