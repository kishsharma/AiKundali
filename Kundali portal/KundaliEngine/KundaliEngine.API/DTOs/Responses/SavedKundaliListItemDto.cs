namespace KundaliEngine.API.DTOs.Responses;

public class SavedKundaliListItemDto
{
    public Guid Id { get; set; }

    public string DisplayName { get; set; } = string.Empty;

    public string FullName { get; set; } = string.Empty;

    public DateOnly DateOfBirth { get; set; }

    public TimeOnly TimeOfBirth { get; set; }

    public string BirthPlace { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }
}
