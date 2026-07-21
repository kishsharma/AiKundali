namespace KundaliEngine.API.Services.ReportShare;

public class ReportShareSmtpOptions
{
    public string Host { get; set; } = "smtp.titan.email";
    public int Port { get; set; } = 587;
    public bool EnableSsl { get; set; } = true;
    public string UserName { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string EncryptedPassword { get; set; } = string.Empty;
    public string PasswordKeyEnvironmentVariable { get; set; } = "KUNDALI_SMTP_PASSWORD_KEY";
    public string FromEmail { get; set; } = string.Empty;
    public string FromName { get; set; } = "Hora Life";
    public int MaxAttachmentBytes { get; set; } = 12 * 1024 * 1024;
    public int TimeoutSeconds { get; set; } = 30;
}
