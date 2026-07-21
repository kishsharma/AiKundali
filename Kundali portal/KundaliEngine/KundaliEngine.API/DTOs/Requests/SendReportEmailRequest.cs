using System.ComponentModel.DataAnnotations;
using System.Text.Json;

namespace KundaliEngine.API.DTOs.Requests;

public class SendReportEmailRequest
{
    [Required]
    [EmailAddress]
    public string ToEmail { get; set; } = string.Empty;

    [MaxLength(200)]
    public string? Subject { get; set; } = "Your Cosmic Hora by Horalife";

    [MaxLength(2000)]
    public string? Message { get; set; } = "Find your Cosmic Hora attached. We hope you enjoy the insights and guidance it provides. For any questions or further assistance, feel free to reach out to us.";

    public JsonElement BirthProfile { get; set; }

    public JsonElement Report { get; set; }

    [MaxLength(180)]
    public string? FileName { get; set; }

    [MaxLength(40)]
    public string? Mode { get; set; }

    [MaxLength(180)]
    public string? AttachmentFileName { get; set; } = "kundali-report.pdf";

    [MaxLength(120)]
    public string? AttachmentContentType { get; set; } = "application/pdf";

    public string? AttachmentBase64 { get; set; }
}

