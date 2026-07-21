using System.ComponentModel.DataAnnotations;
using System.Text.Json;

namespace KundaliEngine.API.DTOs.Requests;

public class ReportPdfRequest
{
    [Required]
    public JsonElement BirthProfile { get; set; }

    [Required]
    public JsonElement Report { get; set; }

    [MaxLength(180)]
    public string? FileName { get; set; }

    [MaxLength(40)]
    public string? Mode { get; set; }
}