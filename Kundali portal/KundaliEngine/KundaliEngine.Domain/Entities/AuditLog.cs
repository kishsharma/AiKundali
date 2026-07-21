namespace KundaliEngine.Domain.Entities;

public class AuditLog
{
    public Guid Id { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public string Action { get; set; } = string.Empty;

    public string Category { get; set; } = string.Empty;

    public Guid? UserId { get; set; }

    public string? UserName { get; set; }

    public string? EntityType { get; set; }

    public string? EntityId { get; set; }

    public string? Description { get; set; }

    public string? IpAddress { get; set; }

    public string? UserAgent { get; set; }

    public string? RequestPath { get; set; }

    public string Status { get; set; } = "Success";

    public string? FailureReason { get; set; }
}
