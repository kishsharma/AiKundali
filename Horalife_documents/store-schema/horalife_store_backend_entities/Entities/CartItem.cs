using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HoraLife.Store.Domain.Entities;

[Table("cart_items", Schema = "store")]
public sealed class CartItem
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; }

    [Column("cart_id")]
    public Guid CartId { get; set; }

    [Column("product_id")]
    public Guid ProductId { get; set; }

    [Column("variant_id")]
    public Guid? VariantId { get; set; }

    [Column("quantity")]
    public int Quantity { get; set; } = 1;

    [Column("unit_price", TypeName = "numeric(12,2)")]
    public decimal UnitPrice { get; set; }

    [Column("created_at")]
    public DateTimeOffset CreatedAt { get; set; }

    [Column("updated_at")]
    public DateTimeOffset UpdatedAt { get; set; }

    public Cart Cart { get; set; } = null!;

    public Product Product { get; set; } = null!;

    public ProductVariant? Variant { get; set; }
}
