namespace KundaliEngine.Domain.Entities;

public class CommunicationLog
{
    public Guid Id { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public string Channel { get; set; } = string.Empty;

    public string? RecipientEmail { get; set; }

    public string? RecipientMobile { get; set; }

    public string MessageType { get; set; } = string.Empty;

    public string? TemplateName { get; set; }

    public string? Subject { get; set; }

    public string Status { get; set; } = "Sent";

    public string? Provider { get; set; }

    public string? ProviderMessageId { get; set; }

    public string? FailureReason { get; set; }

    public string? RelatedEntityType { get; set; }

    public string? RelatedEntityId { get; set; }

    public Guid? TriggeredByUserId { get; set; }

    public string? TriggeredByUserName { get; set; }
}
