using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using HoraLife.Store.Domain.Enums;

namespace HoraLife.Store.Domain.Entities;

[Table("inventory_transactions", Schema = "store")]
public sealed class InventoryTransaction
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; }

    [Column("inventory_id")]
    public Guid InventoryId { get; set; }

    [Required]
    [MaxLength(30)]
    [Column("transaction_type")]
    public InventoryTransactionType TransactionType { get; set; }

    [Column("quantity_change")]
    public int QuantityChange { get; set; }

    [MaxLength(50)]
    [Column("reference_type")]
    public string? ReferenceType { get; set; }

    [Column("reference_id")]
    public Guid? ReferenceId { get; set; }

    [Column("notes")]
    public string? Notes { get; set; }

    [Column("created_at")]
    public DateTimeOffset CreatedAt { get; set; }

    public Inventory Inventory { get; set; } = null!;
}
