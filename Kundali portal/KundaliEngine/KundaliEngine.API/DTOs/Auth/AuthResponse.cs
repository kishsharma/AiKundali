namespace KundaliEngine.API.DTOs.Auth;

public class AuthResponse
{
    public Guid UserId { get; set; }

    public string FullName { get; set; } = string.Empty;

    public string? Email { get; set; }

    public string? PhoneNumber { get; set; }

    public string Role { get; set; } = string.Empty;

    public string Token { get; set; } = string.Empty;
}