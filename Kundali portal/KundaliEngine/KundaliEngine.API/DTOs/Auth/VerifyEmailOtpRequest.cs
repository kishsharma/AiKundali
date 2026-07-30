namespace KundaliEngine.API.DTOs.Auth;

public class VerifyEmailOtpRequest
{
    public Guid ChallengeId { get; set; }

    public string Email { get; set; } = string.Empty;

    public string Code { get; set; } = string.Empty;
}
