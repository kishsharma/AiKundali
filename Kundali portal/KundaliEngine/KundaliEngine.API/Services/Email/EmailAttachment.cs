namespace KundaliEngine.API.Services.Email;

public sealed class EmailAttachment
{
    public required byte[] Bytes { get; init; }

    public required string FileName { get; init; }

    public required string ContentType { get; init; }
}
