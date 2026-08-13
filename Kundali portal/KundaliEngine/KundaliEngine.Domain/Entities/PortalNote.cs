namespace KundaliEngine.Domain.Entities;

public class PortalNote
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public string ContextType { get; set; } = string.Empty;

    public Guid ContextId { get; set; }

    public string Content { get; set; } = string.Empty;

    public string? ContextTitle { get; set; }

    public string? ContextDetailsJson { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public User? User { get; set; }
}
