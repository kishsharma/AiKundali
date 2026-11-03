namespace KundaliEngine.API.Services.Audit;

public sealed class AuditLogEntry
{
    public string Action { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string? EntityType { get; set; }
    public string? EntityId { get; set; }
    public string? Description { get; set; }
    public string Status { get; set; } = "Success";
    public string? FailureReason { get; set; }
    public Guid? UserId { get; set; }
    public string? UserName { get; set; }
    public bool CaptureUserName { get; set; } = true;
    public bool CaptureRequestContext { get; set; } = true;
}
