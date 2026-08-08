using HoraLife.PredictionEngine.Enums;
using HoraLife.PredictionEngine.Facts.HouseAspect;
using HoraLife.PredictionEngine.Facts.PlanetAspect;
using HoraLife.PredictionEngine.Models;

namespace HoraLife.PredictionEngine.Services.Career;
/// <summary>
/// Generates deterministic Career prediction claims from selected chart evidence.
/// </summary>
/// <remarks>
/// CURRENT CAREER RULES
///
/// 1. RuleId: career.10th_house.benefic_aspect
///    Condition:
///    The 10th house receives at least one benefic aspect.
///
///    Result:
///    Supports the claim "career_foundation_support".
///
///    Evidence:
///    - house:10
///    - house:10:benefic_aspect
///
/// 2. RuleId: career.10th_house.malefic_aspect
///    Condition:
///    The 10th house receives at least one malefic aspect.
///
///    Result:
///    Supports the claim "career_foundation_pressure".
///
///    Evidence:
///    - house:10
///    - house:10:malefic_aspect
///
/// 3. Combined condition:
///    When both benefic and malefic aspects influence the 10th house,
///    the engine produces "career_foundation_mixed".
///
/// IMPORTANT:
/// These are HoraLife deterministic engine rules.
/// They are not direct classical scripture references.
/// Classical references should be documented separately after verification.
/// </remarks>
internal sealed class CareerClaimGeneratorService
{
    private const string BeneficAspectRuleId ="career.10th_house.benefic_aspect";

    private const string MaleficAspectRuleId ="career.10th_house.malefic_aspect";

    private const string TenthLordBeneficAspectRuleId ="career.10th_lord.benefic_aspect";

    private const string TenthLordMaleficAspectRuleId ="career.10th_lord.malefic_aspect";

    private const string TenthLordStrongDignityRuleId ="career.10th_lord.strong_dignity";

    private const string TenthLordWeakDignityRuleId ="career.10th_lord.weak_dignity";
    private const string TenthLordInEleventhRuleId ="career.10th_lord.house_11";
    public IReadOnlyList<PredictionClaim> Generate(PredictionRequest request,PredictionEvidenceSet evidence)
    {

        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(evidence);

        
   
        if (request.LifeArea != PredictionLifeArea.Career)
        {
            throw new ArgumentException(
                "This service supports only Career predictions.",
                nameof(request));
        }

        var claims = new List<PredictionClaim>();


        var tenthHouseAspect = evidence.HouseAspects
            .FirstOrDefault(fact => fact.HouseNumber == 10);

        var tenthLordPlacement = evidence.HouseLordPlacements
            .FirstOrDefault(fact => fact.SourceHouse == 10);

        var tenthLordAspect = tenthLordPlacement is null  ? null : evidence.PlanetAspects
             .FirstOrDefault(fact => fact.Planet.Equals(
                tenthLordPlacement.LordPlanet, StringComparison.OrdinalIgnoreCase));


        if (tenthHouseAspect is not null)
        {
            var ruleIds = new List<string>();

            var evidenceKeys = new List<string>
                {
                    "house:10",
                    tenthHouseAspect.EvidenceKey
                };

            if (tenthHouseAspect.HasBeneficAspect)
            {
                ruleIds.Add(BeneficAspectRuleId);
                evidenceKeys.Add("house:10:benefic_aspect");
            }

            if (tenthHouseAspect.HasMaleficAspect)
            {
                ruleIds.Add(MaleficAspectRuleId);
                evidenceKeys.Add("house:10:malefic_aspect");
            }

            if (ruleIds.Count > 0)
            {
                var tone = ResolveTone(tenthHouseAspect);

                claims.Add(new PredictionClaim
                {
                    Key = ResolveClaimKey(tone),
                    LifeArea = PredictionLifeArea.Career,
                    FocusKey = request.FocusKey,
                    Tone = tone,
                    Stage = PredictionClaimStage.Natal,
                    RuleIds = ruleIds.ToArray(),
                    EvidenceKeys = evidenceKeys.ToArray()
                });
            }
        }

        var tenthLord = tenthLordPlacement is null ? null : evidence.PrimaryHouseLords
                    .FirstOrDefault(planet =>
                    planet.Planet.Equals(
                    tenthLordPlacement.LordPlanet,
                    StringComparison.OrdinalIgnoreCase));


        if (tenthLord is not null &&    tenthLordPlacement is not null && tenthLordAspect is not null)
        {
            var ruleIds = new List<string>();

            // Evidence key format:
            // planet:{PlanetName}                  -> identifies the planet
            // planet:{PlanetName}:as_10th_lord     -> planet is acting as the 10th-house lord
            // planet:{PlanetName}:house:{HouseNo}  -> natal house where that planet is placed
            var evidenceKeys = new List<string>
            {
                $"planet:{tenthLord.Planet}",
                $"planet:{tenthLord.Planet}:as_10th_lord",
                tenthLordPlacement.EvidenceKey
            };

            if (tenthLordAspect.HasBeneficAspect)
            {
                ruleIds.Add(TenthLordBeneficAspectRuleId);
                evidenceKeys.Add(
                    $"planet:{tenthLord.Planet}:benefic_aspect");
            }

            if (tenthLordAspect.HasMaleficAspect)
            {
                ruleIds.Add(TenthLordMaleficAspectRuleId);
                evidenceKeys.Add(
                    $"planet:{tenthLord.Planet}:malefic_aspect");
            }
            ///
         
            if (tenthLord.DetailedStrength.Contains("Exalted", StringComparison.OrdinalIgnoreCase) || tenthLord.DetailedStrength.Contains("OwnSign",StringComparison.OrdinalIgnoreCase))
            {
                claims.Add(new PredictionClaim
                {
                    Key = "career_10th_lord_dignity_support",
                    LifeArea = PredictionLifeArea.Career,
                    FocusKey = request.FocusKey,
                    Tone = PredictionTone.Supportive,
                    Stage = PredictionClaimStage.Natal,
                    RuleIds =
                    [
                        TenthLordStrongDignityRuleId
                    ],
                    EvidenceKeys =
                    [
                        $"planet:{tenthLord.Planet}",
                        $"planet:{tenthLord.Planet}:as_10th_lord",
                        $"planet:{tenthLord.Planet}:dignity:{tenthLord.DetailedStrength}"
                    ]
                });
            }
            else if (tenthLord.DetailedStrength.Contains("Debilitated",StringComparison.OrdinalIgnoreCase))
            {
                claims.Add(new PredictionClaim
                {
                    Key = "career_10th_lord_dignity_pressure",
                    LifeArea = PredictionLifeArea.Career,
                    FocusKey = request.FocusKey,
                    Tone = PredictionTone.Challenging,
                    Stage = PredictionClaimStage.Natal,
                    RuleIds =
                    [
                        TenthLordWeakDignityRuleId
                    ],
                    EvidenceKeys =
                    [
                        $"planet:{tenthLord.Planet}",
                        $"planet:{tenthLord.Planet}:as_10th_lord",
                        $"planet:{tenthLord.Planet}:dignity:{tenthLord.DetailedStrength}"
                    ]
                });
            }
            /// 

            if (ruleIds.Count > 0)
            {
                var tone = ResolvePlanetTone(tenthLordAspect);

                claims.Add(new PredictionClaim
                {
                    Key = ResolveTenthLordClaimKey(tone),
                    LifeArea = PredictionLifeArea.Career,
                    FocusKey = request.FocusKey,
                    Tone = tone,
                    Stage = PredictionClaimStage.Natal,
                    RuleIds = ruleIds.ToArray(),
                    EvidenceKeys = evidenceKeys.ToArray()
                });
            }
        }

        return claims;
    }
    private static PredictionTone ResolveTone(HouseAspectFact fact)
    {
        if (fact.HasBeneficAspect &&
            fact.HasMaleficAspect)
        {
            return PredictionTone.Mixed;
        }

        return fact.HasBeneficAspect
            ? PredictionTone.Supportive
            : PredictionTone.Challenging;
    }

    private static string ResolveClaimKey(PredictionTone tone)
    {
        return tone switch
        {
            PredictionTone.Supportive =>
                "career_foundation_support",

            PredictionTone.Challenging =>
                "career_foundation_pressure",

            PredictionTone.Mixed =>
                "career_foundation_mixed",

            _ => throw new InvalidOperationException(
                $"Unsupported Career tone: {tone}.")
        };
    }

    private static PredictionTone ResolvePlanetTone( PlanetAspectFact fact)
    {
        if (fact.HasBeneficAspect &&
            fact.HasMaleficAspect)
        {
            return PredictionTone.Mixed;
        }

        return fact.HasBeneficAspect
            ? PredictionTone.Supportive
            : PredictionTone.Challenging;
    }

    private static string ResolveTenthLordClaimKey(PredictionTone tone)
    {
        return tone switch
        {
            PredictionTone.Supportive =>
                "career_10th_lord_support",

            PredictionTone.Challenging =>
                "career_10th_lord_pressure",

            PredictionTone.Mixed =>
                "career_10th_lord_mixed",

            _ => throw new InvalidOperationException(
                $"Unsupported 10th-lord Career tone: {tone}.")
        };
    }

}