namespace KundaliEngine.API.Services.Dhruva;

public sealed class DhruvaNarrationOptions
{
    public string BaseUrl { get; set; } = "https://api.groq.com/openai/v1";

    public string Model { get; set; } = "openai/gpt-oss-120b";

    public string ApiKey { get; set; } = string.Empty;

    public string SystemPromptPath { get; set; } = "Prompts/dhruva_system.txt";

    public double Temperature { get; set; } = 0.4;

    public int TimeoutSeconds { get; set; } = 45;

    public string ReasoningEffort { get; set; } = "low";
}
