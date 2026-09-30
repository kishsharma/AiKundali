using System.Text.Json;
using KundaliEngine.KnowledgeBase.Models;

namespace KundaliEngine.KnowledgeBase.Services;

public sealed class DivisionalChartMetadataService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly DivisionalChartMetadataDocument _document;

    public DivisionalChartMetadataService()
        : this(AppContext.BaseDirectory)
    {
    }

    public DivisionalChartMetadataService(string basePath)
    {
        var path = Path.Combine(basePath, "Data", "DivisionalCharts", "divisional-chart-metadata.json");
        if (!File.Exists(path))
        {
            throw new FileNotFoundException($"Divisional chart metadata was not found: {path}", path);
        }

        var json = File.ReadAllText(path);
        _document = JsonSerializer.Deserialize<DivisionalChartMetadataDocument>(json, JsonOptions)
            ?? throw new InvalidOperationException($"Divisional chart metadata is empty: {path}");
    }

    public IReadOnlyList<DivisionalChartDefinition> GetCharts() => _document.DivisionalCharts;

    public IReadOnlyList<KeyValuePair<string, PredictionAreaDefinition>> GetAreas() =>
        _document.PredictionAreas.ToList();

    public bool TryGetArea(string key, out PredictionAreaDefinition area)
    {
        foreach (var pair in _document.PredictionAreas)
        {
            if (pair.Key.Equals(key, StringComparison.OrdinalIgnoreCase) && pair.Value is not null)
            {
                area = pair.Value;
                return true;
            }
        }

        area = new PredictionAreaDefinition();
        return false;
    }
}
