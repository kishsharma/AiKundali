using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using HoraLife.Store.Domain.Enums;

namespace HoraLife.Store.Domain.Entities;

[Table("carts", Schema = "store")]
public sealed class Cart
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; }

    [Column("user_id")]
    public Guid? UserId { get; set; }

    [MaxLength(150)]
    [Column("guest_token")]
    public string? GuestToken { get; set; }

    [Required]
    [MaxLength(20)]
    [Column("status")]
    public CartStatus Status { get; set; } = CartStatus.Active;

    [Required]
    [MaxLength(3)]
    [Column("currency", TypeName = "char(3)")]
    public string Currency { get; set; } = "INR";

    [Column("created_at")]
    public DateTimeOffset CreatedAt { get; set; }

    [Column("updated_at")]
    public DateTimeOffset UpdatedAt { get; set; }

    public ICollection<CartItem> Items { get; set; } = new List<CartItem>();

    public ICollection<Order> Orders { get; set; } = new List<Order>();
}
