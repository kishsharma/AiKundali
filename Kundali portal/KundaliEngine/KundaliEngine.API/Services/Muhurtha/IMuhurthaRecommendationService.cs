using KundaliEngine.Domain.Models.Muhurtha;

namespace KundaliEngine.API.Services.Muhurtha;

public interface IMuhurthaRecommendationService
{
    IReadOnlyList<MuhurthaRecommendation> RankAndMerge(
        IReadOnlyList<MuhurthaCandidateSlot> candidates);
}
