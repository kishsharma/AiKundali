using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HoraLife.Store.Domain.Entities;

[Table("order_items", Schema = "store")]
public sealed class OrderItem
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; }

    [Column("order_id")]
    public Guid OrderId { get; set; }

    [Column("product_id")]
    public Guid? ProductId { get; set; }

    [Column("variant_id")]
    public Guid? VariantId { get; set; }

    [Required]
    [MaxLength(250)]
    [Column("product_name")]
    public string ProductName { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    [Column("sku")]
    public string Sku { get; set; } = string.Empty;

    [MaxLength(200)]
    [Column("variant_name")]
    public string? VariantName { get; set; }

    [Column("quantity")]
    public int Quantity { get; set; }

    [Column("unit_price", TypeName = "numeric(12,2)")]
    public decimal UnitPrice { get; set; }

    [Column("discount_amount", TypeName = "numeric(12,2)")]
    public decimal DiscountAmount { get; set; }

    [Column("tax_amount", TypeName = "numeric(12,2)")]
    public decimal TaxAmount { get; set; }

    [Column("line_total", TypeName = "numeric(12,2)")]
    public decimal LineTotal { get; set; }

    [Column("recommendation_id")]
    public Guid? RecommendationId { get; set; }

    public Order Order { get; set; } = null!;

    public Product? Product { get; set; }

    public ProductVariant? Variant { get; set; }

    public UserRecommendation? Recommendation { get; set; }
}
