using KundaliEngine.Domain.Models.Muhurtha;

namespace KundaliEngine.API.Services.Muhurtha;

public interface IMuhurthaRuleEvaluationService
{
    Task<IReadOnlyList<MuhurthaRuleResult>> EvaluateAsync(
        MuhurthaRuleContext context,
        MuhurthaCandidateSlot candidate,
        CancellationToken cancellationToken = default);
}
