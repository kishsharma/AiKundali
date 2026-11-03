namespace KundaliEngine.API.DTOs.Auth;

public class CompleteOtpRegistrationRequest
{
    public string RegistrationToken { get; set; } = string.Empty;

    public string FullName { get; set; } = string.Empty;

    public string? Email { get; set; }

    public string? PhoneNumber { get; set; }

    public string? PreferredLanguage { get; set; }

    public bool RememberMe { get; set; }
}
