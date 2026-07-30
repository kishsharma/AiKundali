using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using HoraLife.Store.Domain.Enums;

namespace HoraLife.Store.Domain.Entities;

[Table("customer_addresses", Schema = "store")]
public sealed class CustomerAddress
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; }

    [Column("user_id")]
    public Guid UserId { get; set; }

    [Required]
    [MaxLength(20)]
    [Column("address_type")]
    public AddressType AddressType { get; set; } = AddressType.Shipping;

    [Required]
    [MaxLength(200)]
    [Column("full_name")]
    public string FullName { get; set; } = string.Empty;

    [Required]
    [MaxLength(30)]
    [Column("phone")]
    public string Phone { get; set; } = string.Empty;

    [Required]
    [MaxLength(250)]
    [Column("address_line1")]
    public string AddressLine1 { get; set; } = string.Empty;

    [MaxLength(250)]
    [Column("address_line2")]
    public string? AddressLine2 { get; set; }

    [Required]
    [MaxLength(100)]
    [Column("city")]
    public string City { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    [Column("state")]
    public string State { get; set; } = string.Empty;

    [Required]
    [MaxLength(20)]
    [Column("postal_code")]
    public string PostalCode { get; set; } = string.Empty;

    [Required]
    [MaxLength(2)]
    [Column("country_code", TypeName = "char(2)")]
    public string CountryCode { get; set; } = "IN";

    [Column("is_default")]
    public bool IsDefault { get; set; }

    [Column("created_at")]
    public DateTimeOffset CreatedAt { get; set; }

    [Column("updated_at")]
    public DateTimeOffset UpdatedAt { get; set; }
}
