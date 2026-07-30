namespace KundaliEngine.Domain.Entities;

public class AuthOtpChallenge
{
    public Guid Id { get; set; }

    public string Purpose { get; set; } = string.Empty;

    public string Channel { get; set; } = "Email";

    public string DestinationNormalized { get; set; } = string.Empty;

    public Guid? UserId { get; set; }

    public string CodeHash { get; set; } = string.Empty;

    public DateTime ExpiresAt { get; set; }

    public DateTime? VerifiedAt { get; set; }

    public DateTime? ConsumedAt { get; set; }

    public int AttemptCount { get; set; }

    public int ResendCount { get; set; }

    public DateTime LastSentAt { get; set; }

    public string? RegistrationTokenHash { get; set; }

    public DateTime? RegistrationTokenExpiresAt { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public string? IpAddress { get; set; }

    public string? UserAgent { get; set; }

    public User? User { get; set; }
}
