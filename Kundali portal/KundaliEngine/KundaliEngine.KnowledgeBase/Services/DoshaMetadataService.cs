using KundaliEngine.Domain.Models;
using System.Text.Json;

namespace KundaliEngine.KnowledgeBase.Services
{
    public class DoshaMetadataService
    {
        private readonly Dictionary<string, DoshaMeta> _doshas;

        public DoshaMetadataService(string jsonPath)
        {
            var json = File.ReadAllText(jsonPath);
            var root = JsonSerializer.Deserialize<DoshaJsonRoot>(json);
            _doshas = root?.Items?.ToDictionary(d => d.Key, d => d) ?? new();
        }

        public DoshaMeta? GetDosha(string key) => _doshas.TryGetValue(key, out var dosha) ? dosha : null;
        public IEnumerable<DoshaMeta> GetAllDoshas() => _doshas.Values;
    }

    public class DoshaJsonRoot
    {
        public string Version { get; set; } = string.Empty;
        public string Note { get; set; } = string.Empty;
        public List<DoshaMeta> Items { get; set; } = new();
    }
}
