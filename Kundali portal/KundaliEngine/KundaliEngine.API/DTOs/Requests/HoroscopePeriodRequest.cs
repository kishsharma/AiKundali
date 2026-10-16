using System.ComponentModel.DataAnnotations;
using KundaliEngine.API.Services.Horoscope;

namespace KundaliEngine.API.DTOs.Requests;

public sealed class HoroscopePeriodRequest
{
    [Required]
    public Guid KundaliId { get; set; }

    /// <summary>Anchor date for the period (local calendar day).</summary>
    [Required]
    public DateOnly AnchorDate { get; set; }

    public HoroscopeHorizon Horizon { get; set; } = HoroscopeHorizon.Daily;

    /// <summary>Optional override; defaults to saved kundali coordinates.</summary>
    public double? Latitude { get; set; }

    public double? Longitude { get; set; }

    public string? Timezone { get; set; }
}
