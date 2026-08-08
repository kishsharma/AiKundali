using HoraLife.PredictionEngine.Enums;
using HoraLife.PredictionEngine.Interpretation;
using HoraLife.PredictionEngine.Models;

namespace HoraLife.PredictionEngine.Synthesis;

internal sealed class PredictionSynthesisService
{
    private readonly HouseLordPlacementInterpreterService _houseLordInterpreter;

    public PredictionSynthesisService( HouseLordPlacementInterpreterService houseLordInterpreter)
    {
        _houseLordInterpreter = houseLordInterpreter;
    }

    public PredictionSynthesis Build(PredictionRequest request, PredictionEvidenceSet evidence, IReadOnlyList<PredictionClaim> claims)
    {
        var consolidated = claims
            .LastOrDefault(x =>
                x.Stage == PredictionClaimStage.Consolidated);

        var interpretations = _houseLordInterpreter.InterpretForLifeArea(request.LifeArea, evidence.HouseLordPlacements);

        return new PredictionSynthesis
        {
            OverallTone = consolidated?.Tone,

            EffectiveFrom = consolidated?.StartDate,

            EffectiveTo = consolidated?.EndDate,

            HouseConnections = interpretations
                .Select(x => new PredictionHouseConnection
                {
                    SourceHouse = x.SourceHouse,
                    LordPlanet = x.LordPlanet,
                    TargetHouse = x.TargetHouse,
                    SourceSignificationKeys =
                        x.SourceSignificationKeys,
                    TargetSignificationKeys =
                        x.TargetSignificationKeys,
                    EvidenceKey = x.EvidenceKey
                })
                .ToArray()
        };
    }
}