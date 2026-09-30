namespace KundaliEngine.API.DTOs.Feedback;

public class SubmitProductFeedbackRequest
{
    public string? Surface { get; set; }

    public string? ContextKey { get; set; }

    public string? Sentiment { get; set; }

    public string? Topic { get; set; }

    public string? Note { get; set; }
}
