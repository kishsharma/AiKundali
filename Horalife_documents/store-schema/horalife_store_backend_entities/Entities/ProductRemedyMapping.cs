using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace HoraLife.Store.Domain.Entities;

[PrimaryKey(nameof(ProductId), nameof(RemedyKeyId))]
[Table("product_remedy_mappings", Schema = "store")]
public sealed class ProductRemedyMapping
{
    [Column("product_id")]
    public Guid ProductId { get; set; }

    [Column("remedy_key_id")]
    public Guid RemedyKeyId { get; set; }

    [Column("recommendation_rank")]
    public int RecommendationRank { get; set; } = 100;

    [Column("match_strength", TypeName = "numeric(5,2)")]
    public decimal MatchStrength { get; set; } = 50;

    [Column("notes")]
    public string? Notes { get; set; }

    public Product Product { get; set; } = null!;

    public RemedyKey RemedyKey { get; set; } = null!;
}
