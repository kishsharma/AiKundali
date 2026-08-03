using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HoraLife.Store.Domain.Entities;

[Table("user_recommendations", Schema = "store")]
public sealed class UserRecommendation
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; }

    // Existing HoraLife user table reference.
    [Column("user_id")]
    public Guid UserId { get; set; }

    // Existing HoraLife Kundali table reference.
    [Column("kundali_id")]
    public Guid KundaliId { get; set; }

    [Column("remedy_key_id")]
    public Guid RemedyKeyId { get; set; }

    [MaxLength(100)]
    [Column("reason_code")]
    public string? ReasonCode { get; set; }

    [MaxLength(500)]
    [Column("display_reason")]
    public string? DisplayReason { get; set; }

    [Column("confidence_score", TypeName = "numeric(5,2)")]
    public decimal? ConfidenceScore { get; set; }

    [Column("valid_until")]
    public DateTimeOffset? ValidUntil { get; set; }

    [Column("created_at")]
    public DateTimeOffset CreatedAt { get; set; }

    public RemedyKey RemedyKey { get; set; } = null!;

    public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
}
