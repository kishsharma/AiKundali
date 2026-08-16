using HoraLife.PredictionEngine.Enums;
using HoraLife.PredictionEngine.Models;

namespace HoraLife.PredictionEngine.Services;

internal sealed class PredictionQuestionAnalyzerService
{
    private static readonly int[] SupportiveDivisionalHouses = [1, 4, 5, 7, 9, 10];
    private static readonly int[] ChallengingDivisionalHouses = [6, 8, 12];
    private static readonly string[] SupportiveOccupantPlanets = ["Jupiter", "Venus", "Moon", "Mercury"];
    private static readonly string[] ChallengingOccupantPlanets = ["Saturn", "Mars", "Rahu", "Ketu", "Sun"];

    private const string SupportedOutcome = "Supported";
    private const string MixedOutcome = "Mixed";
    private const string ChallengingOutcome = "Challenging";

    private readonly PredictionTimingAnalyzerService _timingAnalyzer = new();

    public PredictionAnswer Analyze(
        PredictionRequest request,
        PredictionAreaProfile profile,
        PredictionEvidenceSet evidence)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(profile);
        ArgumentNullException.ThrowIfNull(evidence);

        return (request.LifeArea, request.FocusKey) switch
        {
            (PredictionLifeArea.Relationships, "marriage_possibility") =>
                AnalyzeMarriagePossibility(evidence),

            (PredictionLifeArea.Relationships, "marriage_timing") =>
                AnalyzeMarriageTiming(request, profile, evidence),

            _ => throw new NotSupportedException(
                $"Custom question '{request.FocusKey}' for life area '{request.LifeArea}' is not supported.")
        };
    }

    private PredictionAnswer AnalyzeMarriageTiming(
        PredictionRequest request,
        PredictionAreaProfile profile,
        PredictionEvidenceSet evidence)
    {
        var natalAnswer = AnalyzeMarriagePossibility(evidence);
        var windows = _timingAnalyzer.Analyze(request, profile, evidence);

        return natalAnswer with
        {
            TimingWindows = windows
        };
    }

    private static PredictionAnswer AnalyzeMarriagePossibility(
        PredictionEvidenceSet evidence)
    {
        var supportiveReasons = new List<string>();
        var challengingReasons = new List<string>();
        var evidenceKeys = new List<string>();

        var majorSupport = 0;
        var majorChallenge = 0;
        var secondarySupport = 0;
        var secondaryChallenge = 0;

        var primaryHouse = evidence.PrimaryHouses
            .OrderBy(house => house.HouseNumber)
            .FirstOrDefault();

        if (primaryHouse is not null)
        {
            evidenceKeys.Add($"house:{primaryHouse.HouseNumber}");

            if (primaryHouse.StrengthScore >= 60)
            {
                majorSupport += 1;
                supportiveReasons.Add("relationships.primary_house.strong");
                evidenceKeys.Add($"house:{primaryHouse.HouseNumber}:strength:{primaryHouse.StrengthScore}");
            }
            else if (primaryHouse.StrengthScore < 40)
            {
                majorChallenge += 1;
                challengingReasons.Add("relationships.primary_house.weak");
                evidenceKeys.Add($"house:{primaryHouse.HouseNumber}:strength:{primaryHouse.StrengthScore}");
            }
        }

        var primaryHouseAspect = primaryHouse is null
            ? null
            : evidence.HouseAspects.FirstOrDefault(fact => fact.HouseNumber == primaryHouse.HouseNumber);

        if (primaryHouseAspect?.HasBeneficAspect == true)
        {
            secondarySupport += 1;
            supportiveReasons.Add("relationships.primary_house.benefic_aspect");
            evidenceKeys.Add($"house:{primaryHouseAspect.HouseNumber}:benefic_aspect");
        }

        if (primaryHouseAspect?.HasMaleficAspect == true)
        {
            secondaryChallenge += 1;
            challengingReasons.Add("relationships.primary_house.malefic_aspect");
            evidenceKeys.Add($"house:{primaryHouseAspect.HouseNumber}:malefic_aspect");
        }

        var primaryHouseLord = ResolvePrimaryHouseLord(evidence, primaryHouse);
        if (primaryHouseLord is not null)
        {
            evidenceKeys.Add($"planet:{primaryHouseLord.Planet}");

            if (HasStrongDignity(primaryHouseLord))
            {
                majorSupport += 1;
                supportiveReasons.Add("relationships.primary_house_lord.strong_dignity");
                evidenceKeys.Add($"planet:{primaryHouseLord.Planet}:dignity:{primaryHouseLord.DetailedStrength}");
            }
            else if (HasWeakDignity(primaryHouseLord))
            {
                majorChallenge += 1;
                challengingReasons.Add("relationships.primary_house_lord.weak_dignity");
                evidenceKeys.Add($"planet:{primaryHouseLord.Planet}:dignity:{primaryHouseLord.DetailedStrength}");
            }

            var lordAspect = evidence.PlanetAspects.FirstOrDefault(fact =>
                fact.Planet.Equals(primaryHouseLord.Planet, StringComparison.OrdinalIgnoreCase));

            if (lordAspect?.HasBeneficAspect == true)
            {
                secondarySupport += 1;
                supportiveReasons.Add("relationships.primary_house_lord.benefic_aspect");
                evidenceKeys.Add($"planet:{primaryHouseLord.Planet}:benefic_aspect");
            }

            if (lordAspect?.HasMaleficAspect == true)
            {
                secondaryChallenge += 1;
                challengingReasons.Add("relationships.primary_house_lord.malefic_aspect");
                evidenceKeys.Add($"planet:{primaryHouseLord.Planet}:malefic_aspect");
            }
        }

        var supportiveSignificators = evidence.NaturalSignificators
            .Where(HasSupportiveSignificatorCondition)
            .Select(planet => planet.Planet)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (supportiveSignificators.Length > 0)
        {
            secondarySupport += 1;
            supportiveReasons.Add("relationships.natural_significator.supportive");
            evidenceKeys.AddRange(supportiveSignificators.Select(planet => $"planet:{planet}:natural_significator"));
        }

        var challengingSignificators = evidence.NaturalSignificators
            .Where(HasChallengingSignificatorCondition)
            .Select(planet => planet.Planet)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (challengingSignificators.Length > 0)
        {
            secondaryChallenge += 1;
            challengingReasons.Add("relationships.natural_significator.challenging");
            evidenceKeys.AddRange(challengingSignificators.Select(planet => $"planet:{planet}:natural_significator"));
        }

        var divisionalConfirmation = ApplyDivisionalConfirmation(
            evidence,
            supportiveReasons,
            challengingReasons,
            evidenceKeys);

        majorSupport += divisionalConfirmation.MajorSupport;
        majorChallenge += divisionalConfirmation.MajorChallenge;
        secondarySupport += divisionalConfirmation.SecondarySupport;
        secondaryChallenge += divisionalConfirmation.SecondaryChallenge;

        var outcome = ResolveOutcome(
            majorSupport,
            majorChallenge,
            secondarySupport,
            secondaryChallenge);

        return new PredictionAnswer
        {
            Outcome = outcome,
            ReasonKeys = supportiveReasons
                .Concat(challengingReasons)
                .Distinct(StringComparer.Ordinal)
                .ToArray(),
            EvidenceKeys = evidenceKeys
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray()
        };
    }

    private static DivisionalConfirmationResult ApplyDivisionalConfirmation(
        PredictionEvidenceSet evidence,
        List<string> supportiveReasons,
        List<string> challengingReasons,
        List<string> evidenceKeys)
    {
        var d9Evidence = evidence.DivisionalFocusEvidence
            .FirstOrDefault(item => item.ChartType.Equals("D9", StringComparison.OrdinalIgnoreCase));

        if (d9Evidence is null)
        {
            return DivisionalConfirmationResult.Empty;
        }

        evidenceKeys.AddRange(d9Evidence.NaturalSignificatorEvidenceKeys);

        var seventhHouse = d9Evidence.Houses
            .FirstOrDefault(house => house.HouseNumber == 7);

        if (seventhHouse is null)
        {
            return DivisionalConfirmationResult.Empty;
        }

        evidenceKeys.Add(seventhHouse.HouseEvidenceKey);
        evidenceKeys.Add(seventhHouse.HouseLordEvidenceKey);
        evidenceKeys.AddRange(seventhHouse.OccupantEvidenceKeys);

        var venus = d9Evidence.NaturalSignificators
            .FirstOrDefault(planet => planet.Planet.Equals("Venus", StringComparison.OrdinalIgnoreCase));

        var result = DivisionalConfirmationResult.Empty;

        if (seventhHouse.HouseLordPlanet?.HouseNumber is int lordHouseNumber)
        {
            if (SupportiveDivisionalHouses.Contains(lordHouseNumber))
            {
                result = result with { MajorSupport = result.MajorSupport + 1 };
            }

            if (ChallengingDivisionalHouses.Contains(lordHouseNumber))
            {
                result = result with { MajorChallenge = result.MajorChallenge + 1 };
            }
        }

        if (venus?.HouseNumber is int venusHouseNumber)
        {
            if (SupportiveDivisionalHouses.Contains(venusHouseNumber))
            {
                result = result with { MajorSupport = result.MajorSupport + 1 };
            }

            if (ChallengingDivisionalHouses.Contains(venusHouseNumber))
            {
                result = result with { MajorChallenge = result.MajorChallenge + 1 };
            }
        }

        if (result.MajorSupport > 0)
        {
            supportiveReasons.Add("relationships.d9.confirmation.supportive");
        }

        if (result.MajorChallenge > 0)
        {
            challengingReasons.Add("relationships.d9.confirmation.challenging");
        }

        var supportiveOccupants = seventhHouse.Occupants
            .Select(occupant => occupant.Planet)
            .Where(planetName => SupportiveOccupantPlanets.Any(planet => string.Equals(planet, planetName, StringComparison.OrdinalIgnoreCase)))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (supportiveOccupants.Length > 0)
        {
            result = result with { SecondarySupport = result.SecondarySupport + 1 };
            supportiveReasons.Add("relationships.d9.occupants.supportive");
        }

        var challengingOccupants = seventhHouse.Occupants
            .Select(occupant => occupant.Planet)
            .Where(planetName => ChallengingOccupantPlanets.Any(planet => string.Equals(planet, planetName, StringComparison.OrdinalIgnoreCase)))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (challengingOccupants.Length > 0)
        {
            result = result with { SecondaryChallenge = result.SecondaryChallenge + 1 };
            challengingReasons.Add("relationships.d9.occupants.challenging");
        }

        return result;
    }

    private static PlanetPredictionData? ResolvePrimaryHouseLord(
        PredictionEvidenceSet evidence,
        HousePredictionData? primaryHouse)
    {
        if (primaryHouse is null)
        {
            return evidence.PrimaryHouseLords.FirstOrDefault();
        }

        return evidence.PrimaryHouseLords.FirstOrDefault(planet =>
                   planet.Planet.Equals(primaryHouse.HouseLord, StringComparison.OrdinalIgnoreCase))
               ?? evidence.PrimaryHouseLords.FirstOrDefault();
    }

    private static string ResolveOutcome(
        int majorSupport,
        int majorChallenge,
        int secondarySupport,
        int secondaryChallenge)
    {
        var foundationDelta = majorSupport - majorChallenge;
        var modifierDelta = secondarySupport - secondaryChallenge;

        if (foundationDelta >= 2)
        {
            return SupportedOutcome;
        }

        if (foundationDelta <= -2)
        {
            return ChallengingOutcome;
        }

        if (foundationDelta == 1)
        {
            return modifierDelta >= -1 ? SupportedOutcome : MixedOutcome;
        }

        if (foundationDelta == -1)
        {
            return modifierDelta <= 1 ? ChallengingOutcome : MixedOutcome;
        }

        if (modifierDelta >= 2)
        {
            return SupportedOutcome;
        }

        if (modifierDelta <= -2)
        {
            return ChallengingOutcome;
        }

        return MixedOutcome;
    }

    private static bool HasStrongDignity(PlanetPredictionData planet)
    {
        return planet.DetailedStrength.Equals("OwnSign", StringComparison.OrdinalIgnoreCase) ||
               planet.DetailedStrength.Equals("Exalted", StringComparison.OrdinalIgnoreCase);
    }

    private static bool HasWeakDignity(PlanetPredictionData planet)
    {
        return planet.DetailedStrength.Equals("Debilitated", StringComparison.OrdinalIgnoreCase);
    }

    private static bool HasSupportiveSignificatorCondition(PlanetPredictionData planet)
    {
        return HasStrongDignity(planet) ||
               (planet.HasBeneficAspect && !planet.HasMaleficAspect);
    }

    private static bool HasChallengingSignificatorCondition(PlanetPredictionData planet)
    {
        return HasWeakDignity(planet) ||
               planet.HasMaleficAspect;
    }

    private sealed record DivisionalConfirmationResult(
        int MajorSupport,
        int MajorChallenge,
        int SecondarySupport,
        int SecondaryChallenge)
    {
        public static readonly DivisionalConfirmationResult Empty = new(0, 0, 0, 0);
    }
}




