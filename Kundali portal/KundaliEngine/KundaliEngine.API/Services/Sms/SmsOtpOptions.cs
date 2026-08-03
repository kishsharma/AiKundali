namespace KundaliEngine.API.Services.Sms;

public sealed class SmsOtpOptions
{
    public string BaseUrl { get; set; } = "https://2factor.in";

    public string ApiKey { get; set; } = string.Empty;

    public string ApiKeyEnvironmentVariable { get; set; } = "TWOFACTOR_API_KEY";

    public string TemplateName { get; set; } = "horalifeotp";

    public int TimeoutSeconds { get; set; } = 15;
}