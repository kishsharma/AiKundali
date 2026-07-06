using KundaliEngine.Domain.Models;
using System.Text.Json;

namespace KundaliEngine.KnowledgeBase.Services
{
    public class YogaMetadataService
    {
        private readonly Dictionary<string, YogaMeta> _yogas;

        public YogaMetadataService(string jsonPath)
        {
            var json = File.ReadAllText(jsonPath);
            var root = JsonSerializer.Deserialize<YogaJsonRoot>(json);
            _yogas = root?.Items?.ToDictionary(y => y.Key, y => y) ?? new();
        }

        public YogaMeta? GetYoga(string key) => _yogas.TryGetValue(key, out var yoga) ? yoga : null;
        public IEnumerable<YogaMeta> GetAllYogas() => _yogas.Values;
    }

    // Helper for root structure in Yogas.json
    public class YogaJsonRoot
    {
        public string Version { get; set; } = string.Empty;
        public string Note { get; set; } = string.Empty;
        public List<YogaMeta> Items { get; set; } = new();
    }
}
