using System.Text.Json;
using System.Text.Json.Serialization;

namespace KundaliEngine.API.Services.Muhurtha.GrahaPravesha;

public interface IGrahaPraveshaProfileProvider
{
    GrahaPraveshaProfile GetProfile();
}

public sealed class GrahaPraveshaProfileProvider : IGrahaPraveshaProfileProvider
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter(), new GrahaPraveshaTimeSpanConverter() }
    };

    private readonly Lazy<GrahaPraveshaProfile> _profile;

    public GrahaPraveshaProfileProvider(string dataDirectory)
    {
        if (string.IsNullOrWhiteSpace(dataDirectory))
        {
            throw new ArgumentException("Muhurtha metadata directory is required.", nameof(dataDirectory));
        }

        _profile = new Lazy<GrahaPraveshaProfile>(() =>
        {
            var path = Path.Combine(dataDirectory, "graha-pravesha-profile.json");
            if (!File.Exists(path))
            {
                throw new FileNotFoundException($"Graha Pravesha profile was not found: {path}", path);
            }

            var json = File.ReadAllText(path);
            return JsonSerializer.Deserialize<GrahaPraveshaProfile>(json, SerializerOptions)
                ?? throw new InvalidOperationException("Graha Pravesha profile JSON is empty.");
        });
    }

    public GrahaPraveshaProfile GetProfile() => _profile.Value;
}

internal sealed class GrahaPraveshaTimeSpanConverter : JsonConverter<TimeSpan>
{
    public override TimeSpan Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.String && TimeSpan.TryParse(reader.GetString(), out var value))
        {
            return value;
        }

        return TimeSpan.FromMinutes(5);
    }

    public override void Write(Utf8JsonWriter writer, TimeSpan value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString());
    }
}
