namespace KundaliEngine.Domain.Entities;

public class DhruvaChatMessage
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public Guid KundaliId { get; set; }

    public string Question { get; set; } = string.Empty;

    public string Response { get; set; } = string.Empty;

    public DateTimeOffset AskedAt { get; set; }
}
