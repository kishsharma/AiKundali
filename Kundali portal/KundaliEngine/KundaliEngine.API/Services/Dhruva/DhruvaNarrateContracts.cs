using System.Text.Json;

namespace KundaliEngine.API.Services.Dhruva;

public sealed class DhruvaNarrateRequestDto
{
    public string Mode { get; set; } = "qa";

    public string? Question { get; set; }

    public string? Locale { get; set; }

    public string? Tone { get; set; }

    public string? KundaliId { get; set; }

    public JsonElement Chart { get; set; }
}

public sealed class DhruvaNarrateResponseDto
{
    public string Narration { get; set; } = string.Empty;

    public bool IsAiGenerated { get; set; } = true;

    public string Model { get; set; } = string.Empty;

    public string PromptVersion { get; set; } = DhruvaNarrationService.PromptVersion;

    public string Disclaimer { get; set; } = DhruvaNarrationService.Disclaimer;

    public int PromptTokens { get; set; }

    public int CompletionTokens { get; set; }

    public int TotalTokens { get; set; }
}

public sealed class DhruvaNarrationUnavailableException : Exception
{
    public DhruvaNarrationUnavailableException(string message, Exception? innerException = null, int? upstreamStatusCode = null)
        : base(message, innerException)
    {
        UpstreamStatusCode = upstreamStatusCode;
    }

    public int? UpstreamStatusCode { get; }
}
