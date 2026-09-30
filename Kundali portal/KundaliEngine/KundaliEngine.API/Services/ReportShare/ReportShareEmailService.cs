using KundaliEngine.API.DTOs.Requests;
using KundaliEngine.API.Services.Email;
using KundaliEngine.API.Services.ReportPdf;
using Microsoft.Extensions.Options;

namespace KundaliEngine.API.Services.ReportShare;

public class ReportShareEmailService : IReportShareEmailService
{
    private readonly ReportShareSmtpOptions _options;
    private readonly IReportPdfService _reportPdfService;
    private readonly IEmailSender _emailSender;

    public ReportShareEmailService(
        IOptions<ReportShareSmtpOptions> options,
        IReportPdfService reportPdfService,
        IEmailSender emailSender)
    {
        _options = options.Value;
        _reportPdfService = reportPdfService;
        _emailSender = emailSender;
    }

    public async Task SendReportEmailAsync(SendReportEmailRequest request, CancellationToken cancellationToken)
    {
        var (attachmentBytes, attachmentFileName, attachmentContentType) = BuildAttachment(request);
        if (attachmentBytes.Length == 0)
        {
            throw new InvalidOperationException("Report attachment is empty.");
        }

        if (attachmentBytes.Length > _options.MaxAttachmentBytes)
        {
            throw new InvalidOperationException("Report attachment is too large to email.");
        }

        var reportFormatLabel = GetReportFormatLabel(request.Mode);
        await _emailSender.SendAsync(new EmailMessage
        {
            ToEmail = request.ToEmail.Trim(),
            Subject = string.IsNullOrWhiteSpace(request.Subject) ? $"{reportFormatLabel} for {GetReportName(request)}" : request.Subject.Trim(),
            TextBody = BuildEmailBody(request.Message, reportFormatLabel),
            Attachments = new[]
            {
                new EmailAttachment
                {
                    Bytes = attachmentBytes,
                    FileName = SanitizeAttachmentFileName(attachmentFileName),
                    ContentType = attachmentContentType
                }
            }
        }, cancellationToken);
    }


    private static string BuildEmailBody(string? message, string reportFormatLabel)
    {
        var body = string.IsNullOrWhiteSpace(message)
            ? $"Please find the {reportFormatLabel} attached."
            : message.Trim();

        const string visitLine = "To explore more personalized Kundali insights, timing guidance, and compatibility reports, please visit https://horalife.in.";
        if (body.Contains("horalife.in", StringComparison.OrdinalIgnoreCase))
        {
            return body;
        }

        return $"{body}{Environment.NewLine}{Environment.NewLine}{visitLine}";
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
            Mode = request.Mode,
            Audience = "paid"
        };

        return (
            _reportPdfService.GenerateReportPdf(pdfRequest),
            _reportPdfService.BuildFileName(pdfRequest),
            "application/pdf");
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

    private static string GetReportFormatLabel(string? mode)
    {
        return (mode ?? string.Empty).Trim().ToLowerInvariant() switch
        {
            "corporate" or "corporate-advisory" => "Personal Vedic Astrology Advisory Report",
            "mobile" or "mobile-friendly" or "mobilefriendly" => "Mobile Friendly Kundali Report",
            "print-bw" or "black-white" or "black-and-white" => "Black & White Kundali Report",
            _ => "Kundali Report"
        };
    }

    private static string GetReportName(SendReportEmailRequest request)
    {
        if (request.BirthProfile.ValueKind == System.Text.Json.JsonValueKind.Object)
        {
            string? givenName = null;
            foreach (var propertyName in new[] { "fullName", "name" })
            {
                if (request.BirthProfile.TryGetProperty(propertyName, out var value))
                {
                    var text = value.ValueKind == System.Text.Json.JsonValueKind.String ? value.GetString() : value.ToString();
                    if (!string.IsNullOrWhiteSpace(text))
                    {
                        givenName = text.Trim();
                        break;
                    }
                }
            }

            string? surname = null;
            foreach (var propertyName in new[] { "surname", "Surname" })
            {
                if (request.BirthProfile.TryGetProperty(propertyName, out var value))
                {
                    var text = value.ValueKind == System.Text.Json.JsonValueKind.String ? value.GetString() : value.ToString();
                    if (!string.IsNullOrWhiteSpace(text))
                    {
                        surname = text.Trim();
                        break;
                    }
                }
            }

            if (!string.IsNullOrWhiteSpace(givenName) && !string.IsNullOrWhiteSpace(surname)
                && !givenName.EndsWith(surname, StringComparison.OrdinalIgnoreCase))
            {
                return $"{givenName} {surname}";
            }

            if (!string.IsNullOrWhiteSpace(givenName))
            {
                return givenName;
            }
        }

        return "Kundali";
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

