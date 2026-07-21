using System.Text.Json;

namespace KundaliEngine.API.Services.CurrentLifeFocus;

public class CurrentLifeFocusMetadataService
{
    private readonly Lazy<CurrentLifeFocusConfiguration> _configuration;

    public CurrentLifeFocusMetadataService(string basePath)
    {
        _configuration = new Lazy<CurrentLifeFocusConfiguration>(() => LoadConfiguration(basePath));
    }

    public virtual CurrentLifeFocusConfiguration GetConfiguration() => _configuration.Value;

    public virtual LifeAreaConfiguration? GetLifeArea(string key)
    {
        return _configuration.Value.LifeAreas.FirstOrDefault(area =>
            string.Equals(area.Key, key, StringComparison.OrdinalIgnoreCase));
    }

    private static CurrentLifeFocusConfiguration LoadConfiguration(string basePath)
    {
        var path = Path.Combine(basePath, "life-areas.json");
        if (!File.Exists(path))
        {
            throw new FileNotFoundException("Current Life Focus metadata file was not found.", path);
        }

        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var config = JsonSerializer.Deserialize<CurrentLifeFocusConfiguration>(File.ReadAllText(path), options)
            ?? throw new InvalidOperationException("Current Life Focus metadata could not be read.");

        Validate(config);
        return config;
    }

    private static void Validate(CurrentLifeFocusConfiguration config)
    {
        if (config.LifeAreas.Count == 0)
        {
            throw new InvalidOperationException("Current Life Focus metadata must define at least one life area.");
        }

        var duplicate = config.LifeAreas
            .GroupBy(area => area.Key, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault(group => group.Count() > 1);
        if (duplicate != null)
        {
            throw new InvalidOperationException($"Duplicate Current Life Focus area key: {duplicate.Key}.");
        }

        foreach (var area in config.LifeAreas)
        {
            if (string.IsNullOrWhiteSpace(area.Key) || string.IsNullOrWhiteSpace(area.DisplayName))
            {
                throw new InvalidOperationException("Every Current Life Focus area must define key and displayName.");
            }

            if (area.Houses.Count == 0 || area.Houses.Any(house => house.House < 1 || house.House > 12 || house.Weight <= 0))
            {
                throw new InvalidOperationException($"Current Life Focus area '{area.Key}' has invalid house metadata.");
            }

            var duplicateConcern = area.FollowUpOptions
                .GroupBy(option => option.Key, StringComparer.OrdinalIgnoreCase)
                .FirstOrDefault(group => group.Count() > 1);
            if (duplicateConcern != null)
            {
                throw new InvalidOperationException($"Current Life Focus area '{area.Key}' has duplicate concern key '{duplicateConcern.Key}'.");
            }
        }
    }
}
