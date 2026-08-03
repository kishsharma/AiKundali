using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HoraLife.Store.Domain.Entities;

[Table("remedy_keys", Schema = "store")]
public sealed class RemedyKey
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; }

    [Required]
    [MaxLength(100)]
    [Column("remedy_key")]
    public string Key { get; set; } = string.Empty;

    [Required]
    [MaxLength(200)]
    [Column("name")]
    public string Name { get; set; } = string.Empty;

    [Required]
    [MaxLength(40)]
    [Column("remedy_type")]
    public string RemedyType { get; set; } = string.Empty;

    [Column("description")]
    public string? Description { get; set; }

    [Column("is_active")]
    public bool IsActive { get; set; } = true;

    [Column("created_at")]
    public DateTimeOffset CreatedAt { get; set; }

    public ICollection<ProductRemedyMapping> ProductMappings { get; set; } = new List<ProductRemedyMapping>();

    public ICollection<UserRecommendation> UserRecommendations { get; set; } = new List<UserRecommendation>();
}
