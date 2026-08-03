using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;
using HoraLife.Store.Domain.Enums;

namespace HoraLife.Store.Domain.Entities;

[Table("products", Schema = "store")]
public sealed class Product
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; }

    [Column("category_id")]
    public Guid CategoryId { get; set; }

    [Required]
    [MaxLength(100)]
    [Column("sku")]
    public string Sku { get; set; } = string.Empty;

    [Required]
    [MaxLength(180)]
    [Column("slug")]
    public string Slug { get; set; } = string.Empty;

    [Required]
    [MaxLength(250)]
    [Column("name")]
    public string Name { get; set; } = string.Empty;

    [MaxLength(500)]
    [Column("short_description")]
    public string? ShortDescription { get; set; }

    [Column("description")]
    public string? Description { get; set; }

    [Required]
    [MaxLength(40)]
    [Column("product_type")]
    public ProductType ProductType { get; set; }

    [Column("base_price", TypeName = "numeric(12,2)")]
    public decimal BasePrice { get; set; }

    [Column("compare_at_price", TypeName = "numeric(12,2)")]
    public decimal? CompareAtPrice { get; set; }

    [Required]
    [MaxLength(3)]
    [Column("currency", TypeName = "char(3)")]
    public string Currency { get; set; } = "INR";

    [Column("attributes", TypeName = "jsonb")]
    public JsonDocument Attributes { get; set; } = JsonDocument.Parse("{}");

    [Column("requires_consultation")]
    public bool RequiresConsultation { get; set; }

    [Column("is_featured")]
    public bool IsFeatured { get; set; }

    [Column("is_active")]
    public bool IsActive { get; set; } = true;

    [Column("created_at")]
    public DateTimeOffset CreatedAt { get; set; }

    [Column("updated_at")]
    public DateTimeOffset UpdatedAt { get; set; }

    public Category Category { get; set; } = null!;

    public ICollection<ProductVariant> Variants { get; set; } = new List<ProductVariant>();

    public ICollection<ProductImage> Images { get; set; } = new List<ProductImage>();

    public ICollection<Inventory> InventoryRecords { get; set; } = new List<Inventory>();

    public ICollection<ProductRemedyMapping> RemedyMappings { get; set; } = new List<ProductRemedyMapping>();

    public ICollection<CartItem> CartItems { get; set; } = new List<CartItem>();

    public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
}
