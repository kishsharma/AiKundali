using System.Net;
using System.Net.Mail;
using KundaliEngine.API.DTOs.Requests;
using KundaliEngine.API.Services.ReportPdf;
using Microsoft.Extensions.Options;

namespace KundaliEngine.API.Services.ReportShare;

public class ReportShareEmailService : IReportShareEmailService
{
    private readonly ReportShareSmtpOptions _options;
    private readonly IReportPdfService _reportPdfService;

    public ReportShareEmailService(
        IOptions<ReportShareSmtpOptions> options,
        IReportPdfService reportPdfService)
    {
        _options = options.Value;
        _reportPdfService = reportPdfService;
    }

    public async Task SendReportEmailAsync(SendReportEmailRequest request, CancellationToken cancellationToken)
    {
        var smtpPassword = GetSmtpPassword();
        EnsureConfigured(smtpPassword);

        var (attachmentBytes, attachmentFileName, attachmentContentType) = BuildAttachment(request);
        if (attachmentBytes.Length == 0)
        {
            throw new InvalidOperationException("Report attachment is empty.");
        }

        if (attachmentBytes.Length > _options.MaxAttachmentBytes)
        {
            throw new InvalidOperationException("Report attachment is too large to email.");
        }

        using var message = new MailMessage
        {
            From = new MailAddress(GetFromEmail(), _options.FromName),
            Subject = string.IsNullOrWhiteSpace(request.Subject) ? "Kundali report" : request.Subject.Trim(),
            Body = string.IsNullOrWhiteSpace(request.Message)
                ? "Please find the Kundali report attached."
                : request.Message.Trim(),
            IsBodyHtml = false
        };

        message.To.Add(new MailAddress(request.ToEmail.Trim()));

        var attachmentStream = new MemoryStream(attachmentBytes);
        var attachment = new Attachment(
            attachmentStream,
            SanitizeAttachmentFileName(attachmentFileName),
            attachmentContentType);
        message.Attachments.Add(attachment);

        var timeoutSeconds = Math.Clamp(_options.TimeoutSeconds, 5, 120);
        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(TimeSpan.FromSeconds(timeoutSeconds));

        using var client = new SmtpClient(_options.Host, _options.Port)
        {
            DeliveryMethod = SmtpDeliveryMethod.Network,
            EnableSsl = _options.EnableSsl,
            UseDefaultCredentials = false,
            Credentials = new NetworkCredential(_options.UserName, smtpPassword),
            Timeout = timeoutSeconds * 1000
        };

        using var registration = timeoutCts.Token.Register(client.SendAsyncCancel);
        await client.SendMailAsync(message, timeoutCts.Token);
    }

    private (byte[] Bytes, string FileName, string ContentType) BuildAttachment(SendReportEmailRequest request)
    {
        if (!string.IsNullOrWhiteSpace(request.AttachmentBase64))
        {
            return (
                DecodeAttachment(request.AttachmentBase64),
                FirstNonEmpty(request.FileName, request.AttachmentFileName, "kundali-report.pdf")!,
                string.IsNullOrWhiteSpace(request.AttachmentContentType) ? "application/pdf" : request.AttachmentContentType!.Trim());
        }

        var pdfRequest = new ReportPdfRequest
        {
            BirthProfile = request.BirthProfile,
            Report = request.Report,
            FileName = FirstNonEmpty(request.FileName, request.AttachmentFileName),
            Mode = request.Mode
        };

        return (
            _reportPdfService.GenerateReportPdf(pdfRequest),
            _reportPdfService.BuildFileName(pdfRequest),
            "application/pdf");
    }

    private void EnsureConfigured(string smtpPassword)
    {
        if (string.IsNullOrWhiteSpace(_options.Host)
            || string.IsNullOrWhiteSpace(_options.UserName)
            || string.IsNullOrWhiteSpace(smtpPassword))
        {
            throw new ReportShareConfigurationException("Report email SMTP settings are not configured.");
        }
    }

    private string GetSmtpPassword()
    {
        if (!string.IsNullOrWhiteSpace(_options.EncryptedPassword))
        {
            var key = Environment.GetEnvironmentVariable(_options.PasswordKeyEnvironmentVariable);
            return EncryptedSecretProtector.Decrypt(_options.EncryptedPassword, key ?? string.Empty);
        }

        return _options.Password;
    }

    private string GetFromEmail()
    {
        return string.IsNullOrWhiteSpace(_options.FromEmail)
            ? _options.UserName
            : _options.FromEmail;
    }

    private static byte[] DecodeAttachment(string attachmentBase64)
    {
        var value = attachmentBase64.Trim();
        var commaIndex = value.IndexOf(',');
        if (value.StartsWith("data:", StringComparison.OrdinalIgnoreCase) && commaIndex >= 0)
        {
            value = value[(commaIndex + 1)..];
        }

        try
        {
            return Convert.FromBase64String(value);
        }
        catch (FormatException ex)
        {
            throw new InvalidOperationException("Report attachment is not valid base64.", ex);
        }
    }

    private static string? FirstNonEmpty(params string?[] values)
    {
        return values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value))?.Trim();
    }

    private static string SanitizeAttachmentFileName(string fileName)
    {
        var sanitized = string.Join("_", fileName.Trim().Split(Path.GetInvalidFileNameChars()));
        return string.IsNullOrWhiteSpace(sanitized) ? "kundali-report.pdf" : sanitized;
    }
}