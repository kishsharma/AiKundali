using KundaliEngine.Domain.Models.Muhurtha;

namespace KundaliEngine.API.Services.Muhurtha;

public interface IMuhurthaCandidateGenerator
{
    Task<IReadOnlyList<MuhurthaCandidateSlot>> GenerateAsync(
        MuhurthaRuleContext context,
        CancellationToken cancellationToken = default);
}
