using System.Text.Json;

namespace KundaliEngine.API.DTOs.Responses;

public class SavedCompatibilityDetailDto
{
    public Guid Id { get; set; }

    public string PersonAName { get; set; } = string.Empty;

    public string? PersonAGender { get; set; }

    public DateOnly PersonADateOfBirth { get; set; }

    public TimeOnly PersonATimeOfBirth { get; set; }

    public string PersonABirthPlace { get; set; } = string.Empty;

    public decimal? PersonALatitude { get; set; }

    public decimal? PersonALongitude { get; set; }

    public string PersonBName { get; set; } = string.Empty;

    public string? PersonBGender { get; set; }

    public DateOnly PersonBDateOfBirth { get; set; }

    public TimeOnly PersonBTimeOfBirth { get; set; }

    public string PersonBBirthPlace { get; set; } = string.Empty;

    public decimal? PersonBLatitude { get; set; }

    public decimal? PersonBLongitude { get; set; }

    public int OverallCompatibilityScore { get; set; }

    public decimal? AshtakootaScore { get; set; }

    public JsonElement Request { get; set; }

    public JsonElement Result { get; set; }

    public DateTime CreatedAt { get; set; }
}