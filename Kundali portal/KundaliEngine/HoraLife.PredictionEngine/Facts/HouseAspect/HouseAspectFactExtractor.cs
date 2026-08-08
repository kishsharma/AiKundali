using HoraLife.PredictionEngine.Models;

namespace HoraLife.PredictionEngine.Facts.HouseAspect;

internal sealed class HouseAspectFactExtractor
{
    public IReadOnlyList<HouseAspectFact> Extract(
        PredictionContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return context.Houses
            .Where(house =>
                house.HasBeneficAspect ||
                house.HasMaleficAspect)
            .Select(house =>
                new HouseAspectFact
                {
                    HouseNumber = house.HouseNumber,
                    HasBeneficAspect = house.HasBeneficAspect,
                    HasMaleficAspect = house.HasMaleficAspect,
                    AspectedBy = house.AspectedBy,

                    // Evidence key format:
                    // house:{HouseNo}:aspect
                    EvidenceKey =
                        $"house:{house.HouseNumber}:aspect"
                })
            .ToArray();
    }
}