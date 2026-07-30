using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HoraLife.Store.Domain.Entities;

[Table("product_images", Schema = "store")]
public sealed class ProductImage
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; }

    [Column("product_id")]
    public Guid ProductId { get; set; }

    [Column("variant_id")]
    public Guid? VariantId { get; set; }

    [Required]
    [Column("image_url")]
    public string ImageUrl { get; set; } = string.Empty;

    [MaxLength(250)]
    [Column("alt_text")]
    public string? AltText { get; set; }

    [Column("display_order")]
    public int DisplayOrder { get; set; }

    [Column("is_primary")]
    public bool IsPrimary { get; set; }

    [Column("created_at")]
    public DateTimeOffset CreatedAt { get; set; }

    public Product Product { get; set; } = null!;

    public ProductVariant? Variant { get; set; }
}
