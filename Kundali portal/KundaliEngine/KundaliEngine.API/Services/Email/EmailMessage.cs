namespace KundaliEngine.API.Services.Email;

public sealed class EmailMessage
{
    public required string ToEmail { get; init; }

    public required string Subject { get; init; }

    public string? TextBody { get; init; }

    public string? HtmlBody { get; init; }

    public IReadOnlyCollection<EmailAttachment> Attachments { get; init; } = Array.Empty<EmailAttachment>();
}
