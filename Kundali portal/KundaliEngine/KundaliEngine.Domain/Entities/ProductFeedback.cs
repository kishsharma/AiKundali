namespace KundaliEngine.Domain.Entities;

public class ProductFeedback
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public string Surface { get; set; } = string.Empty;

    public string ContextKey { get; set; } = string.Empty;

    public string Sentiment { get; set; } = string.Empty;

    public string? Topic { get; set; }

    public string? Note { get; set; }

    public string PlanCode { get; set; } = "Free";

    public DateOnly FeedbackDate { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
