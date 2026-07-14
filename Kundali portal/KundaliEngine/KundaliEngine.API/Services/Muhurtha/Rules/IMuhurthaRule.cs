using KundaliEngine.Domain.Models.Muhurtha;

namespace KundaliEngine.API.Services.Muhurtha.Rules;

public interface IMuhurthaRule
{
    string Key { get; }

    Task<MuhurthaRuleResult> EvaluateAsync(
        MuhurthaRuleContext context,
        MuhurthaCandidateSlot candidate,
        CancellationToken cancellationToken = default);
}
