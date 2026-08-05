using HoraLife.PredictionEngine.Enums;
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
    private const string BeneficAspectRuleId =
        "career.10th_house.benefic_aspect";

    private const string MaleficAspectRuleId =
        "career.10th_house.malefic_aspect";

    public IReadOnlyList<PredictionClaim> Generate( PredictionRequest request, PredictionEvidenceSet evidence)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(evidence);

        if (request.LifeArea != PredictionLifeArea.Career)
        {
            throw new ArgumentException("This service supports only Career predictions.", nameof(request));
        }

        var tenthHouse = evidence.PrimaryHouses .FirstOrDefault(house => house.HouseNumber == 10);

        if (tenthHouse is null)
        {
            return Array.Empty<PredictionClaim>();
        }

        var ruleIds = new List<string>();
        var evidenceKeys = new List<string>
        {
            "house:10"
        };

        if (tenthHouse.HasBeneficAspect)
        {
            ruleIds.Add(BeneficAspectRuleId);
            evidenceKeys.Add("house:10:benefic_aspect");
        }

        if (tenthHouse.HasMaleficAspect)
        {
            ruleIds.Add(MaleficAspectRuleId);
            evidenceKeys.Add("house:10:malefic_aspect");
        }

        if (ruleIds.Count == 0)
        {
            return Array.Empty<PredictionClaim>();
        }

        var tone = ResolveTone(tenthHouse);

        return
        [
            new PredictionClaim
            {
                Key = ResolveClaimKey(tone),
                LifeArea = PredictionLifeArea.Career,
                FocusKey = request.FocusKey,
                Tone = tone,
                Stage = PredictionClaimStage.Natal,
                RuleIds = ruleIds.ToArray(),
                EvidenceKeys = evidenceKeys.ToArray()
            }
        ];
    }

    private static PredictionTone ResolveTone(
        HousePredictionData tenthHouse)
    {
        if (tenthHouse.HasBeneficAspect &&
            tenthHouse.HasMaleficAspect)
        {
            return PredictionTone.Mixed;
        }

        return tenthHouse.HasBeneficAspect
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
}