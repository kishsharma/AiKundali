namespace KundaliEngine.API.DTOs.User;

public class UpdatePracticeProfileRequest
{
    public List<string> Services { get; set; } = new();

    public string? Note { get; set; }
}
