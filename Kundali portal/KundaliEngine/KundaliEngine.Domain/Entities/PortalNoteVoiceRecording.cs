namespace KundaliEngine.Domain.Entities;

public class PortalNoteVoiceRecording
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public string ContextType { get; set; } = string.Empty;

    public Guid ContextId { get; set; }

    public string FileName { get; set; } = string.Empty;

    public string ContentType { get; set; } = string.Empty;

    public string StoragePath { get; set; } = string.Empty;

    public int? DurationSeconds { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public User? User { get; set; }
}
