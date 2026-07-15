using KundaliEngine.Domain.Enums;
using KundaliEngine.Domain.Models.Muhurtha;

namespace KundaliEngine.API.Services.Muhurtha;

public class MuhurthaRuleConfigurationProvider : IMuhurthaRuleConfigurationProvider
{
    private readonly MuhurthaMetadataService _metadataService;

    public MuhurthaRuleConfigurationProvider(MuhurthaMetadataService metadataService)
    {
        _metadataService = metadataService;
    }

    public Task<IReadOnlyList<MuhurthaRuleConfiguration>> GetRulesAsync(
        MuhurthaEventType eventType,
        MuhurthaRegionalProfileType regionalProfile,
        CancellationToken cancellationToken = default)
    {
        var rules = _metadataService.GetRuleConfigurations(eventType, regionalProfile);
        return Task.FromResult(rules);
    }
}

