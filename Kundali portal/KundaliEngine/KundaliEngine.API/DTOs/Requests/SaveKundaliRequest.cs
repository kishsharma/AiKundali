using System.Text.Json;

namespace KundaliEngine.API.DTOs.Requests;

public class SaveKundaliRequest
{
    public Guid? ClientId { get; set; }
    public string Name { get; set; } = string.Empty;

    public string? Surname { get; set; }

    public string? FatherName { get; set; }

    public string? MotherName { get; set; }

    public string? Gotra { get; set; }

    public string? Gender { get; set; }

    public DateTime BirthDateTime { get; set; }

    public string BirthPlace { get; set; } = string.Empty;

    public double Latitude { get; set; }

    public double Longitude { get; set; }

    public double TimeZoneOffset { get; set; }

    public JsonElement ChartData { get; set; }

    /// <summary>
    /// ask (default), update, create, or reuse when the same birth moment is already saved.
    /// </summary>
    public string OnDuplicate { get; set; } = "ask";
}
