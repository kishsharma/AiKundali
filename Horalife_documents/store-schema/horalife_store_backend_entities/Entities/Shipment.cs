using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;
using HoraLife.Store.Domain.Enums;

namespace HoraLife.Store.Domain.Entities;

[Table("shipments", Schema = "store")]
public sealed class Shipment
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; }

    [Column("order_id")]
    public Guid OrderId { get; set; }

    [MaxLength(100)]
    [Column("courier_provider")]
    public string? CourierProvider { get; set; }

    [MaxLength(150)]
    [Column("tracking_number")]
    public string? TrackingNumber { get; set; }

    [Column("tracking_url")]
    public string? TrackingUrl { get; set; }

    [Required]
    [MaxLength(30)]
    [Column("shipment_status")]
    public ShipmentStatus ShipmentStatus { get; set; } = ShipmentStatus.Pending;

    [Column("shipped_at")]
    public DateTimeOffset? ShippedAt { get; set; }

    [Column("delivered_at")]
    public DateTimeOffset? DeliveredAt { get; set; }

    [Column("estimated_delivery")]
    public DateOnly? EstimatedDelivery { get; set; }

    [Column("provider_response", TypeName = "jsonb")]
    public JsonDocument? ProviderResponse { get; set; }

    [Column("created_at")]
    public DateTimeOffset CreatedAt { get; set; }

    [Column("updated_at")]
    public DateTimeOffset UpdatedAt { get; set; }

    public Order Order { get; set; } = null!;
}
