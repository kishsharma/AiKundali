namespace KundaliEngine.API.DTOs.Auth;

public class AuthResponse
{
    public Guid UserId { get; set; }

    public string FullName { get; set; } = string.Empty;

    public string? Email { get; set; }

    public string? PhoneNumber { get; set; }

    public string? PreferredLanguage { get; set; }

    public string Role { get; set; } = string.Empty;

    public string PlanCode { get; set; } = "Free";

    public DateTimeOffset? PlanExpiresAt { get; set; }

    public bool IsPremium { get; set; }

    public bool IsPaid { get; set; }

    public bool IsPlus { get; set; }

    public int KundaliSaveLimit { get; set; }

    public int MatchSaveLimit { get; set; }

    public string Token { get; set; } = string.Empty;
}
