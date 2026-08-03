using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;
using HoraLife.Store.Domain.Enums;

namespace HoraLife.Store.Domain.Entities;

[Table("orders", Schema = "store")]
public sealed class Order
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; }

    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Column("order_number")]
    public long OrderNumber { get; set; }

    [Column("user_id")]
    public Guid? UserId { get; set; }

    [Column("cart_id")]
    public Guid? CartId { get; set; }

    [Required]
    [MaxLength(250)]
    [Column("customer_email")]
    public string CustomerEmail { get; set; } = string.Empty;

    [MaxLength(30)]
    [Column("customer_phone")]
    public string? CustomerPhone { get; set; }

    [Required]
    [MaxLength(30)]
    [Column("order_status")]
    public OrderStatus OrderStatus { get; set; } = OrderStatus.Pending;

    [Required]
    [MaxLength(30)]
    [Column("payment_status")]
    public OrderPaymentStatus PaymentStatus { get; set; } = OrderPaymentStatus.Pending;

    [Column("subtotal_amount", TypeName = "numeric(12,2)")]
    public decimal SubtotalAmount { get; set; }

    [Column("discount_amount", TypeName = "numeric(12,2)")]
    public decimal DiscountAmount { get; set; }

    [Column("shipping_amount", TypeName = "numeric(12,2)")]
    public decimal ShippingAmount { get; set; }

    [Column("tax_amount", TypeName = "numeric(12,2)")]
    public decimal TaxAmount { get; set; }

    [Column("total_amount", TypeName = "numeric(12,2)")]
    public decimal TotalAmount { get; set; }

    [Required]
    [MaxLength(3)]
    [Column("currency", TypeName = "char(3)")]
    public string Currency { get; set; } = "INR";

    [Column("shipping_address", TypeName = "jsonb")]
    public JsonDocument ShippingAddress { get; set; } = JsonDocument.Parse("{}");

    [Column("billing_address", TypeName = "jsonb")]
    public JsonDocument? BillingAddress { get; set; }

    [Required]
    [MaxLength(40)]
    [Column("order_source")]
    public OrderSource OrderSource { get; set; } = OrderSource.DirectStore;

    [Column("recommended_kundali_id")]
    public Guid? RecommendedKundaliId { get; set; }

    [Column("notes")]
    public string? Notes { get; set; }

    [Column("placed_at")]
    public DateTimeOffset PlacedAt { get; set; }

    [Column("updated_at")]
    public DateTimeOffset UpdatedAt { get; set; }

    public Cart? Cart { get; set; }

    public ICollection<OrderItem> Items { get; set; } = new List<OrderItem>();

    public ICollection<Payment> Payments { get; set; } = new List<Payment>();

    public ICollection<Shipment> Shipments { get; set; } = new List<Shipment>();
}
