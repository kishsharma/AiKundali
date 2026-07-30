using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;

namespace HoraLife.Store.Domain.Entities;

[Table("product_variants", Schema = "store")]
public sealed class ProductVariant
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; }

    [Column("product_id")]
    public Guid ProductId { get; set; }

    [Required]
    [MaxLength(100)]
    [Column("sku")]
    public string Sku { get; set; } = string.Empty;

    [Required]
    [MaxLength(200)]
    [Column("name")]
    public string Name { get; set; } = string.Empty;

    [Column("attributes", TypeName = "jsonb")]
    public JsonDocument Attributes { get; set; } = JsonDocument.Parse("{}");

    [Column("price", TypeName = "numeric(12,2)")]
    public decimal Price { get; set; }

    [Column("compare_at_price", TypeName = "numeric(12,2)")]
    public decimal? CompareAtPrice { get; set; }

    [Column("weight_grams", TypeName = "numeric(10,3)")]
    public decimal? WeightGrams { get; set; }

    [Column("is_active")]
    public bool IsActive { get; set; } = true;

    [Column("created_at")]
    public DateTimeOffset CreatedAt { get; set; }

    [Column("updated_at")]
    public DateTimeOffset UpdatedAt { get; set; }

    public Product Product { get; set; } = null!;

    public ICollection<ProductImage> Images { get; set; } = new List<ProductImage>();

    public ICollection<Inventory> InventoryRecords { get; set; } = new List<Inventory>();

    public ICollection<CartItem> CartItems { get; set; } = new List<CartItem>();

    public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
}
