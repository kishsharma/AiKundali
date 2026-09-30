namespace KundaliEngine.Domain.Entities;

public class DhruvaQuestionLog
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public DateTimeOffset AskedAt { get; set; }

    public bool IsAiMode { get; set; }

    public string Kind { get; set; } = "custom";

    public string Language { get; set; } = "english";

    public int PromptTokens { get; set; }

    public int CompletionTokens { get; set; }

    public int TotalTokens { get; set; }
}
