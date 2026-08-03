using System.Text.Json;

namespace KundaliEngine.API.DTOs.Requests;

public class SaveCompatibilityRequest
{
    public CompatibilityRequestDto Request { get; set; } = new();

    public JsonElement Result { get; set; }
}