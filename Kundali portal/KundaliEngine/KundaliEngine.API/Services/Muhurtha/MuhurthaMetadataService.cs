using System.Text.Json;
using System.Text.Json.Serialization;
using KundaliEngine.Domain.Enums;
using KundaliEngine.Domain.Models.Muhurtha;

namespace KundaliEngine.API.Services.Muhurtha;

public class MuhurthaMetadataService
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly Lazy<IReadOnlyList<MuhurthaEventConfiguration>> _eventConfigurations;
    private readonly Lazy<IReadOnlyList<MuhurthaRegionalProfile>> _regionalProfiles;
    private readonly Lazy<IReadOnlyList<MuhurthaRuleDefinition>> _ruleDefinitions;
    private readonly Lazy<IReadOnlyList<MuhurthaRuleConfiguration>> _ruleConfigurations;
    private readonly Lazy<MuhurthaScoringConfiguration> _scoringConfiguration;
    private readonly Lazy<IReadOnlyList<MuhurthaPanchangaSuitabilityConfiguration>> _panchangaSuitabilityConfigurations;
    private readonly Lazy<IReadOnlyList<MuhurthaVastuEligibilityRuleConfiguration>> _vastuEligibilityRules;

    public MuhurthaMetadataService(string dataDirectory)
    {
        if (string.IsNullOrWhiteSpace(dataDirectory))
        {
            throw new ArgumentException("Muhurtha metadata directory is required.", nameof(dataDirectory));
        }

        DataDirectory = dataDirectory;
        _eventConfigurations = new Lazy<IReadOnlyList<MuhurthaEventConfiguration>>(
            () => LoadList<MuhurthaEventConfiguration>("event-types.json"));
        _regionalProfiles = new Lazy<IReadOnlyList<MuhurthaRegionalProfile>>(
            () => LoadList<MuhurthaRegionalProfile>("regional-profiles.json"));
        _ruleDefinitions = new Lazy<IReadOnlyList<MuhurthaRuleDefinition>>(
            () => LoadList<MuhurthaRuleDefinition>("rule-definitions.json"));
        _ruleConfigurations = new Lazy<IReadOnlyList<MuhurthaRuleConfiguration>>(
            () => LoadList<MuhurthaRuleConfiguration>("event-rule-configs.json"));
        _scoringConfiguration = new Lazy<MuhurthaScoringConfiguration>(
            () => Load<MuhurthaScoringConfiguration>("scoring-config.json") ?? new MuhurthaScoringConfiguration());
        _panchangaSuitabilityConfigurations = new Lazy<IReadOnlyList<MuhurthaPanchangaSuitabilityConfiguration>>(
            () => LoadList<MuhurthaPanchangaSuitabilityConfiguration>("panchanga-suitability.json"));
        _vastuEligibilityRules = new Lazy<IReadOnlyList<MuhurthaVastuEligibilityRuleConfiguration>>(
            () => LoadList<MuhurthaVastuEligibilityRuleConfiguration>("vastu-eligibility-rules.json"));
    }

    public string DataDirectory { get; }

    public IReadOnlyList<MuhurthaRuleConfiguration> GetRuleConfigurations(
        MuhurthaEventType eventType,
        MuhurthaRegionalProfileType regionalProfile)
    {
        return _ruleConfigurations.Value
            .Where(config => IsApplicable(config, eventType, regionalProfile))
            .ToList();
    }

    public MuhurthaScoringConfiguration GetScoringConfiguration() => _scoringConfiguration.Value;

    public MuhurthaPanchangaSuitabilityConfiguration? GetPanchangaSuitability(MuhurthaEventType eventType)
    {
        return _panchangaSuitabilityConfigurations.Value.FirstOrDefault(config => config.EventType == eventType)
            ?? _panchangaSuitabilityConfigurations.Value.FirstOrDefault(config => config.EventType == MuhurthaEventType.GeneralAuspiciousWork);
    }

    public IReadOnlyList<MuhurthaVastuEligibilityRuleConfiguration> GetVastuEligibilityRules(MuhurthaEventType eventType)
    {
        return _vastuEligibilityRules.Value
            .Where(rule => rule.Enabled
                && (rule.ApplicableEventTypes.Count == 0 || rule.ApplicableEventTypes.Contains(eventType)))
            .ToList();
    }

    public MuhurthaEventConfiguration? GetEventConfiguration(MuhurthaEventType eventType)
    {
        return _eventConfigurations.Value.FirstOrDefault(config => config.EventType == eventType);
    }

    public MuhurthaRegionalProfile? GetRegionalProfile(MuhurthaRegionalProfileType profileType)
    {
        return _regionalProfiles.Value.FirstOrDefault(profile => profile.ProfileType == profileType);
    }

    private static bool IsApplicable(
        MuhurthaRuleConfiguration configuration,
        MuhurthaEventType eventType,
        MuhurthaRegionalProfileType regionalProfile)
    {
        if (!configuration.Enabled || configuration.ExcludedEventTypes.Contains(eventType))
        {
            return false;
        }

        var eventMatches = configuration.ApplicableEventTypes.Count == 0
            || configuration.ApplicableEventTypes.Contains(eventType);
        var regionMatches = configuration.RegionalProfiles.Count == 0
            || configuration.RegionalProfiles.Contains(regionalProfile);

        return eventMatches && regionMatches;
    }

    private IReadOnlyList<T> LoadList<T>(string fileName)
    {
        return Load<List<T>>(fileName) ?? new List<T>();
    }

    private T? Load<T>(string fileName)
    {
        var path = Path.Combine(DataDirectory, fileName);
        if (!File.Exists(path))
        {
            throw new FileNotFoundException($"Muhurtha metadata file not found: {path}", path);
        }

        var json = File.ReadAllText(path);
        return JsonSerializer.Deserialize<T>(json, SerializerOptions);
    }
}





