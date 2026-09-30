using System.Text.Json;

namespace KundaliEngine.KnowledgeBase.Services
{
    public class RashiMetadataService
    {
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        private static readonly IReadOnlyDictionary<string, string> SignFileMap =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["Aries"] = "Mesha",
                ["Taurus"] = "Vrishabha",
                ["Gemini"] = "Mithuna",
                ["Cancer"] = "Karka",
                ["Leo"] = "Simha",
                ["Virgo"] = "Kanya",
                ["Libra"] = "Tula",
                ["Scorpio"] = "Vrischika",
                ["Sagittarius"] = "Dhanu",
                ["Capricorn"] = "Makara",
                ["Aquarius"] = "Kumbha",
                ["Pisces"] = "Meena"
            };

        private readonly string _rashiDataPath;

        public RashiMetadataService(string rashiDataPath)
        {
            _rashiDataPath = rashiDataPath;
        }

        public RashiMeta? GetRashiBySign(string signName)
        {
            if (string.IsNullOrWhiteSpace(signName))
            {
                return null;
            }

            var fileName = SignFileMap.TryGetValue(signName, out var mappedName)
                ? mappedName
                : signName;

            var path = Path.Combine(_rashiDataPath, $"{fileName}.json");
            if (!File.Exists(path))
            {
                return null;
            }

            var json = File.ReadAllText(path);
            return JsonSerializer.Deserialize<RashiMeta>(json, JsonOptions);
        }

        public sealed class RashiMeta
        {
            public string Name { get; set; } = string.Empty;
            public string EnglishName { get; set; } = string.Empty;
            public string Symbol { get; set; } = string.Empty;
            public string Element { get; set; } = string.Empty;
            public string Ruler { get; set; } = string.Empty;
            public List<string> Strengths { get; set; } = new();
            public List<string> Weaknesses { get; set; } = new();
            public string Gemstone { get; set; } = string.Empty;
            public string Metal { get; set; } = string.Empty;
            public string Color { get; set; } = string.Empty;
            public List<int> LuckyNumbers { get; set; } = new();
            public string FavorableDay { get; set; } = string.Empty;
            public string FavorableDirection { get; set; } = string.Empty;
            public GhatChakraMeta? GhatChakra { get; set; }
        }

        public sealed class GhatChakraMeta
        {
            public string Month { get; set; } = string.Empty;
            public string Tithi { get; set; } = string.Empty;
            public string TithiNumbers { get; set; } = string.Empty;
            public string Weekday { get; set; } = string.Empty;
            public string Nakshatra { get; set; } = string.Empty;
            public string Yoga { get; set; } = string.Empty;
            public string Karana { get; set; } = string.Empty;
            public string MoonMale { get; set; } = string.Empty;
            public string MoonFemale { get; set; } = string.Empty;
        }
    }
}
