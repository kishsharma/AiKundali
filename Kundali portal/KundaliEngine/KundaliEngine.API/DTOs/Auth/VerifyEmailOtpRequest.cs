namespace KundaliEngine.API.DTOs.Auth;

public class VerifyEmailOtpRequest
{
    public Guid ChallengeId { get; set; }

    public string? Channel { get; set; }

    public string? Email { get; set; }

    public string? PhoneNumber { get; set; }

    public string Code { get; set; } = string.Empty;

    public bool RememberMe { get; set; }
}
