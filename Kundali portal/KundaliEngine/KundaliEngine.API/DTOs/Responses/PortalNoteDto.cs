namespace KundaliEngine.API.DTOs.Responses;

public sealed class PortalNoteDto
{
    public Guid? Id { get; set; }

    public string ContextType { get; set; } = string.Empty;

    public Guid? ContextId { get; set; }

    public string Content { get; set; } = string.Empty;

    public string? ContextTitle { get; set; }

    public List<string> ContextDetails { get; set; } = new();

    public int VoiceRecordingCount { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }
}
