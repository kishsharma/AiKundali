using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;
using HoraLife.Store.Domain.Enums;

namespace HoraLife.Store.Domain.Entities;

[Table("payments", Schema = "store")]
public sealed class Payment
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; }

    [Column("order_id")]
    public Guid OrderId { get; set; }

    [Required]
    [MaxLength(50)]
    [Column("provider")]
    public string Provider { get; set; } = string.Empty;

    [MaxLength(200)]
    [Column("provider_payment_id")]
    public string? ProviderPaymentId { get; set; }

    [MaxLength(200)]
    [Column("provider_order_id")]
    public string? ProviderOrderId { get; set; }

    [MaxLength(200)]
    [Column("provider_event_id")]
    public string? ProviderEventId { get; set; }

    [Required]
    [MaxLength(30)]
    [Column("payment_status")]
    public PaymentStatus PaymentStatus { get; set; } = PaymentStatus.Pending;

    [Column("amount", TypeName = "numeric(12,2)")]
    public decimal Amount { get; set; }

    [Required]
    [MaxLength(3)]
    [Column("currency", TypeName = "char(3)")]
    public string Currency { get; set; } = "INR";

    [MaxLength(50)]
    [Column("payment_method")]
    public string? PaymentMethod { get; set; }

    [Column("gateway_response", TypeName = "jsonb")]
    public JsonDocument? GatewayResponse { get; set; }

    [Column("created_at")]
    public DateTimeOffset CreatedAt { get; set; }

    [Column("updated_at")]
    public DateTimeOffset UpdatedAt { get; set; }

    public Order Order { get; set; } = null!;
}
