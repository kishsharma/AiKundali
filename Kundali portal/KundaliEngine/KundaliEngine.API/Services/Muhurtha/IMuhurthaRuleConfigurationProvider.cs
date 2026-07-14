using KundaliEngine.Domain.Enums;
using KundaliEngine.Domain.Models.Muhurtha;

namespace KundaliEngine.API.Services.Muhurtha;

public interface IMuhurthaRuleConfigurationProvider
{
    Task<IReadOnlyList<MuhurthaRuleConfiguration>> GetRulesAsync(
        MuhurthaEventType eventType,
        MuhurthaRegionalProfileType regionalProfile,
        CancellationToken cancellationToken = default);
}
