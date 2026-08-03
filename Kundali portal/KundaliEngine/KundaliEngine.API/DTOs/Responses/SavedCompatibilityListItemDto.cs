namespace KundaliEngine.API.DTOs.Responses;

public class SavedCompatibilityListItemDto
{
    public Guid Id { get; set; }

    public string DisplayName { get; set; } = string.Empty;

    public string PersonAName { get; set; } = string.Empty;

    public DateOnly PersonADateOfBirth { get; set; }

    public string PersonABirthPlace { get; set; } = string.Empty;

    public string PersonBName { get; set; } = string.Empty;

    public DateOnly PersonBDateOfBirth { get; set; }

    public string PersonBBirthPlace { get; set; } = string.Empty;

    public int OverallCompatibilityScore { get; set; }

    public decimal? AshtakootaScore { get; set; }

    public DateTime CreatedAt { get; set; }
}