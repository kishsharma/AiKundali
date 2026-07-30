namespace KundaliEngine.API.DTOs.Auth;

public class CompleteOtpRegistrationRequest
{
    public string RegistrationToken { get; set; } = string.Empty;

    public string FullName { get; set; } = string.Empty;

    public string? PhoneNumber { get; set; }
}
