using System.Text.Json;

namespace KundaliEngine.KnowledgeBase.Services
{
    public class YoniCompatibilityService
    {
        private readonly YoniCompatibilityMeta _meta;

        public YoniCompatibilityService(string jsonPath)
        {
            var json = File.ReadAllText(jsonPath);
            _meta = JsonSerializer.Deserialize<YoniCompatibilityMeta>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            }) ?? new YoniCompatibilityMeta();
        }

        public double GetScore(string boyYoni, string girlYoni)
        {
            if (string.IsNullOrWhiteSpace(boyYoni) || string.IsNullOrWhiteSpace(girlYoni))
            {
                return _meta.NeutralScore;
            }

            if (boyYoni.Equals(girlYoni, StringComparison.OrdinalIgnoreCase))
            {
                return _meta.SameScore;
            }

            if (IsPairIn(_meta.FriendlyPairs, boyYoni, girlYoni))
            {
                return _meta.FriendlyScore;
            }

            if (IsPairIn(_meta.UnfriendlyPairs, boyYoni, girlYoni))
            {
                return _meta.UnfriendlyScore;
            }

            return IsPairIn(_meta.EnemyPairs, boyYoni, girlYoni) ? _meta.EnemyScore : _meta.NeutralScore;
        }

        private static bool IsPairIn(List<List<string>> pairs, string first, string second)
        {
            return pairs.Any(pair =>
                pair.Count == 2 &&
                ((pair[0].Equals(first, StringComparison.OrdinalIgnoreCase) &&
                  pair[1].Equals(second, StringComparison.OrdinalIgnoreCase)) ||
                 (pair[0].Equals(second, StringComparison.OrdinalIgnoreCase) &&
                  pair[1].Equals(first, StringComparison.OrdinalIgnoreCase))));
        }

        private sealed class YoniCompatibilityMeta
        {
            public double SameScore { get; set; } = 4;
            public double FriendlyScore { get; set; } = 3;
            public double NeutralScore { get; set; } = 2;
            public double UnfriendlyScore { get; set; } = 1;
            public double EnemyScore { get; set; } = 0;
            public List<List<string>> FriendlyPairs { get; set; } = new();
            public List<List<string>> UnfriendlyPairs { get; set; } = new();
            public List<List<string>> EnemyPairs { get; set; } = new();
        }
    }
}
