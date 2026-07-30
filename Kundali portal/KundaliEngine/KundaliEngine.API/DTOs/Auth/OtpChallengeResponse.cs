namespace KundaliEngine.API.DTOs.Auth;

public class OtpChallengeResponse
{
    public Guid ChallengeId { get; set; }

    public DateTime ExpiresAt { get; set; }

    public int ResendAfterSeconds { get; set; }

    public string MaskedDestination { get; set; } = string.Empty;

    public string Message { get; set; } = string.Empty;
}
