namespace KundaliEngine.API.DTOs.Auth;

public class VerifyRegistrationOtpResponse
{
    public string RegistrationToken { get; set; } = string.Empty;

    public DateTime ExpiresAt { get; set; }

    public string? Email { get; set; }

    public string? PhoneNumber { get; set; }
}
