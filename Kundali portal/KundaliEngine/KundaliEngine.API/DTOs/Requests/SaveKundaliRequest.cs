using System.Text.Json;

namespace KundaliEngine.API.DTOs.Requests;

public class SaveKundaliRequest
{
    public string Name { get; set; } = string.Empty;

    public string? Gender { get; set; }

    public DateTime BirthDateTime { get; set; }

    public string BirthPlace { get; set; } = string.Empty;

    public double Latitude { get; set; }

    public double Longitude { get; set; }

    public double TimeZoneOffset { get; set; }

    public JsonElement ChartData { get; set; }
}
