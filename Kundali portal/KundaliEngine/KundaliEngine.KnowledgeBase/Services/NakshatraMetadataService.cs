using System.Text.Json;
using System.Text.Json.Serialization;

namespace KundaliEngine.KnowledgeBase.Services
{
    public class NakshatraMetadataService
    {
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        private readonly string _nakshatraDataPath;

        public NakshatraMetadataService(string nakshatraDataPath)
        {
            _nakshatraDataPath = nakshatraDataPath;
        }

        public string GetPadaAkshara(string nakshatraName, int pada)
        {
            if (string.IsNullOrWhiteSpace(nakshatraName) || pada < 1 || pada > 4)
            {
                return string.Empty;
            }

            var path = Path.Combine(_nakshatraDataPath, $"{nakshatraName}.json");
            if (!File.Exists(path))
            {
                return string.Empty;
            }

            var json = File.ReadAllText(path);
            var root = JsonSerializer.Deserialize<NakshatraMeta>(json, JsonOptions);
            return root?.Padas.FirstOrDefault(p => p.Number == pada)?.Akshara ?? string.Empty;
        }

        public NakshatraMeta? GetNakshatra(string nakshatraName)
        {
            if (string.IsNullOrWhiteSpace(nakshatraName))
            {
                return null;
            }

            var path = Path.Combine(_nakshatraDataPath, $"{nakshatraName}.json");
            if (!File.Exists(path))
            {
                return null;
            }

            var json = File.ReadAllText(path);
            return JsonSerializer.Deserialize<NakshatraMeta>(json, JsonOptions);
        }

        public sealed class NakshatraMeta
        {
            public List<NakshatraPadaMeta> Padas { get; set; } = new();
            public string Lord { get; set; } = string.Empty;
            public string Symbol { get; set; } = string.Empty;
            public string Gana { get; set; } = string.Empty;
            public string Yoni { get; set; } = string.Empty;
            public string Nadi { get; set; } = string.Empty;
            public string Varna { get; set; } = string.Empty;
            [JsonConverter(typeof(FlexibleStringListConverter))]
            public List<string> Vashya { get; set; } = new();
            public Dictionary<string, string?>? Dosha { get; set; } = new();
            public string FastingDay { get; set; } = string.Empty;
            public List<string> BusinessSuitability { get; set; } = new();
        }

        public sealed class NakshatraPadaMeta
        {
            public int Number { get; set; }
            public string Akshara { get; set; } = string.Empty;
        }

        private sealed class FlexibleStringListConverter : JsonConverter<List<string>>
        {
            public override List<string> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            {
                if (reader.TokenType == JsonTokenType.String)
                {
                    var value = reader.GetString();
                    return string.IsNullOrWhiteSpace(value) ? new List<string>() : new List<string> { value };
                }

                if (reader.TokenType == JsonTokenType.StartArray)
                {
                    var values = JsonSerializer.Deserialize<List<string>>(ref reader, options);
                    return values?.Where(value => !string.IsNullOrWhiteSpace(value)).ToList() ?? new List<string>();
                }

                if (reader.TokenType == JsonTokenType.Null)
                {
                    return new List<string>();
                }

                using var document = JsonDocument.ParseValue(ref reader);
                return new List<string> { document.RootElement.ToString() };
            }

            public override void Write(Utf8JsonWriter writer, List<string> value, JsonSerializerOptions options)
            {
                JsonSerializer.Serialize(writer, value, options);
            }
        }
    }
}
