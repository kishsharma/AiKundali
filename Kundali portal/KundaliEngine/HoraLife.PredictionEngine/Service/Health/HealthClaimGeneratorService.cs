using HoraLife.PredictionEngine.Enums;
using HoraLife.PredictionEngine.Facts.HouseAspect;
using HoraLife.PredictionEngine.Facts.HouseLordAspect;
using HoraLife.PredictionEngine.Facts.PlanetAspect;
using HoraLife.PredictionEngine.Models;

namespace HoraLife.PredictionEngine.Services.Health;

internal sealed class HealthClaimGeneratorService
{
    public IReadOnlyList<PredictionClaim> Generate(PredictionRequest request, PredictionEvidenceSet evidence)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(evidence);

        if (request.LifeArea != PredictionLifeArea.Health)
        {
            throw new ArgumentException(
                "This service supports only Health predictions.",
                nameof(request));
        }

        var claims = new List<PredictionClaim>();

        var primaryHouseNumbers = evidence.PrimaryHouses
            .Select(x => x.HouseNumber)
            .ToHashSet();

        foreach (var fact in evidence.HouseAspects
                     .Where(x => primaryHouseNumbers.Contains(x.HouseNumber)))
        {
            var ruleIds = new List<string>();
            var evidenceKeys = new List<string>
            {
                $"house:{fact.HouseNumber}",
                fact.EvidenceKey
            };

            if (fact.HasBeneficAspect)
            {
                ruleIds.Add(
                    $"health.house_{fact.HouseNumber}.benefic_aspect");

                evidenceKeys.Add(
                    $"house:{fact.HouseNumber}:benefic_aspect");
            }

            if (fact.HasMaleficAspect)
            {
                ruleIds.Add(
                    $"health.house_{fact.HouseNumber}.malefic_aspect");

                evidenceKeys.Add(
                    $"house:{fact.HouseNumber}:malefic_aspect");
            }

            if (ruleIds.Count == 0)
            {
                continue;
            }

            var tone = ResolveTone(fact);

            claims.Add(new PredictionClaim
            {
                Key = ResolveClaimKey(
                    fact.HouseNumber,
                    tone),

                LifeArea = PredictionLifeArea.Health,
                FocusKey = request.FocusKey,
                Tone = tone,
                Stage = PredictionClaimStage.Natal,
                RuleIds = ruleIds.ToArray(),
                EvidenceKeys = evidenceKeys.ToArray()
            });
        }

        foreach (var house in evidence.PrimaryHouses)
        {
            AddPrimaryHouseLordClaims(
                claims,
                request,
                evidence,
                house.HouseNumber);
        }

        return claims;
    }

    private static void AddPrimaryHouseLordClaims(
        List<PredictionClaim> claims,
        PredictionRequest request,
        PredictionEvidenceSet evidence,
        int houseNumber)
    {
        var lordPlacement = evidence.HouseLordPlacements
            .FirstOrDefault(fact => fact.SourceHouse == houseNumber);

        if (lordPlacement is null)
        {
            return;
        }

        var lord = evidence.PrimaryHouseLords
            .FirstOrDefault(planet =>
                planet.Planet.Equals(
                    lordPlacement.LordPlanet,
                    StringComparison.OrdinalIgnoreCase));

        if (lord is null)
        {
            return;
        }

        var lordAspect = evidence.PlanetAspects
            .FirstOrDefault(fact =>
                fact.Planet.Equals(
                    lordPlacement.LordPlanet,
                    StringComparison.OrdinalIgnoreCase));

        if (lordAspect is not null)
        {
            AddPrimaryHouseLordAspectClaim(
                claims,
                request,
                houseNumber,
                lord,
                lordPlacement,
                lordAspect);
        }

        AddPrimaryHouseLordDignityClaim(
            claims,
            request,
            houseNumber,
            lord);
    }

    private static void AddPrimaryHouseLordAspectClaim(
        List<PredictionClaim> claims,
        PredictionRequest request,
        int houseNumber,
        PlanetPredictionData lord,
        HouseLordPlacementFact lordPlacement,
        PlanetAspectFact lordAspect)
    {
        var ruleIds = new List<string>();

        var evidenceKeys = new List<string>
        {
            $"planet:{lord.Planet}",
            $"planet:{lord.Planet}:as_{houseNumber}_lord",
            lordPlacement.EvidenceKey
        };

        if (lordAspect.HasBeneficAspect)
        {
            ruleIds.Add(
                $"health.house_{houseNumber}_lord.benefic_aspect");

            evidenceKeys.Add(
                $"planet:{lord.Planet}:benefic_aspect");
        }

        if (lordAspect.HasMaleficAspect)
        {
            ruleIds.Add(
                $"health.house_{houseNumber}_lord.malefic_aspect");

            evidenceKeys.Add(
                $"planet:{lord.Planet}:malefic_aspect");
        }

        if (ruleIds.Count == 0)
        {
            return;
        }

        var tone = ResolvePlanetTone(lordAspect);

        claims.Add(new PredictionClaim
        {
            Key = ResolveHouseLordClaimKey(
                houseNumber,
                tone),

            LifeArea = PredictionLifeArea.Health,
            FocusKey = request.FocusKey,
            Tone = tone,
            Stage = PredictionClaimStage.Natal,
            RuleIds = ruleIds.ToArray(),
            EvidenceKeys = evidenceKeys.ToArray()
        });
    }

    private static void AddPrimaryHouseLordDignityClaim(
        List<PredictionClaim> claims,
        PredictionRequest request,
        int houseNumber,
        PlanetPredictionData lord)
    {
        PredictionTone? tone = null;
        string? ruleId = null;

        if (lord.DetailedStrength.Contains(
                "Exalted",
                StringComparison.OrdinalIgnoreCase) ||
            lord.DetailedStrength.Contains(
                "OwnSign",
                StringComparison.OrdinalIgnoreCase))
        {
            tone = PredictionTone.Supportive;
            ruleId = $"health.house_{houseNumber}_lord.strong_dignity";
        }
        else if (lord.DetailedStrength.Contains(
                     "Debilitated",
                     StringComparison.OrdinalIgnoreCase))
        {
            tone = PredictionTone.Challenging;
            ruleId = $"health.house_{houseNumber}_lord.weak_dignity";
        }

        if (tone is null ||
            ruleId is null)
        {
            return;
        }

        claims.Add(new PredictionClaim
        {
            Key = ResolveHouseLordClaimKey(
                houseNumber,
                tone.Value),

            LifeArea = PredictionLifeArea.Health,
            FocusKey = request.FocusKey,
            Tone = tone.Value,
            Stage = PredictionClaimStage.Natal,
            RuleIds =
            [
                ruleId
            ],
            EvidenceKeys =
            [
                $"planet:{lord.Planet}",
                $"planet:{lord.Planet}:as_{houseNumber}_lord",
                $"planet:{lord.Planet}:dignity:{lord.DetailedStrength}"
            ]
        });
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

    private static PredictionTone ResolvePlanetTone(PlanetAspectFact fact)
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

    private static string ResolveClaimKey(int houseNumber, PredictionTone tone)
    {
        return $"health_{ResolveHouseFocusKey(houseNumber)}_{ResolveToneKey(tone)}";
    }

    private static string ResolveHouseLordClaimKey(int houseNumber, PredictionTone tone)
    {
        return $"health_{ResolveHouseFocusKey(houseNumber)}_lord_{ResolveToneKey(tone)}";
    }

    private static string ResolveHouseFocusKey(int houseNumber)
    {
        return houseNumber switch
        {
            1 => "vitality",

            _ => throw new InvalidOperationException(
                $"Unsupported Health primary house: {houseNumber}.")
        };
    }

    private static string ResolveToneKey(PredictionTone tone)
    {
        return tone switch
        {
            PredictionTone.Supportive => "support",
            PredictionTone.Challenging => "pressure",
            PredictionTone.Mixed => "mixed",

            _ => throw new InvalidOperationException(
                $"Unsupported Health tone: {tone}.")
        };
    }
}
