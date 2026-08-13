namespace KundaliEngine.API.DTOs.Responses;

public sealed class PortalNoteVoiceRecordingDto
{
    public Guid Id { get; set; }

    public string ContextType { get; set; } = string.Empty;

    public Guid? ContextId { get; set; }

    public string FileName { get; set; } = string.Empty;

    public string ContentType { get; set; } = string.Empty;

    public int? DurationSeconds { get; set; }

    public DateTime CreatedAt { get; set; }
}
