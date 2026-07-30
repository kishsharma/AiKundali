using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HoraLife.Store.Domain.Entities;

[Table("inventory", Schema = "store")]
public sealed class Inventory
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; }

    [Column("product_id")]
    public Guid ProductId { get; set; }

    [Column("variant_id")]
    public Guid? VariantId { get; set; }

    [Column("stock_quantity")]
    public int StockQuantity { get; set; }

    [Column("reserved_quantity")]
    public int ReservedQuantity { get; set; }

    [Column("reorder_level")]
    public int ReorderLevel { get; set; }

    [Column("updated_at")]
    public DateTimeOffset UpdatedAt { get; set; }

    [NotMapped]
    public int AvailableQuantity => StockQuantity - ReservedQuantity;

    public Product Product { get; set; } = null!;

    public ProductVariant? Variant { get; set; }

    public ICollection<InventoryTransaction> Transactions { get; set; } = new List<InventoryTransaction>();
}
