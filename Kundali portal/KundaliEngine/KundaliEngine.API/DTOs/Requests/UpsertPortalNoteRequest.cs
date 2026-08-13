namespace KundaliEngine.API.DTOs.Requests;

public sealed class UpsertPortalNoteRequest
{
    public string ContextType { get; set; } = string.Empty;

    public Guid? ContextId { get; set; }

    public string Content { get; set; } = string.Empty;
}
