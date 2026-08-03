namespace KundaliEngine.API.DTOs.Auth;

public class StartEmailOtpRequest
{
    public string? Channel { get; set; }

    public string? Email { get; set; }

    public string? PhoneNumber { get; set; }
}
