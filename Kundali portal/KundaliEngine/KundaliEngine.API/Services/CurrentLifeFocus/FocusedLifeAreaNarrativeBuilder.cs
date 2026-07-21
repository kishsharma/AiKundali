using KundaliEngine.API.DTOs.Responses;
using KundaliEngine.Domain.Enums;

namespace KundaliEngine.API.Services.CurrentLifeFocus;

public interface IFocusedLifeAreaNarrativeBuilder
{
    FocusedLifeAreaNarrativeDto Build(
        LifeAreaConfiguration areaConfig,
        LifeAreaActivation area,
        CurrentLifeFocusSnapshot snapshot,
        IReadOnlyList<FocusedActivatedHouseDto> activatedHouses,
        IReadOnlyList<string> possibleExperiences,
        IReadOnlyList<string> supportiveFactors,
        IReadOnlyList<string> pressureFactors);
}

public class FocusedLifeAreaNarrativeBuilder : IFocusedLifeAreaNarrativeBuilder
{
    public FocusedLifeAreaNarrativeDto Build(
        LifeAreaConfiguration areaConfig,
        LifeAreaActivation area,
        CurrentLifeFocusSnapshot snapshot,
        IReadOnlyList<FocusedActivatedHouseDto> activatedHouses,
        IReadOnlyList<string> possibleExperiences,
        IReadOnlyList<string> supportiveFactors,
        IReadOnlyList<string> pressureFactors)
    {
        var topHouses = activatedHouses
            .OrderByDescending(house => house.ActivationScore)
            .ThenBy(house => house.House)
            .Take(3)
            .ToList();
        var topHouseText = topHouses.Count == 0
            ? "the relevant natal houses"
            : string.Join(", ", topHouses.Select(house => $"house {house.House} ({house.Role.ToLowerInvariant()})"));
        var dashaText = BuildDashaText(snapshot.Mahadasha, snapshot.Antardasha);
        var situation = possibleExperiences.FirstOrDefault()
            ?? $"{areaConfig.DisplayName} may feel more noticeable in daily life.";
        var supportText = supportiveFactors.FirstOrDefault();
        var pressureText = pressureFactors.FirstOrDefault();
        var toneText = area.Tone.ToLowerInvariant();
        var activationText = area.ActivationLevel.ToLowerInvariant();

        return new FocusedLifeAreaNarrativeDto
        {
            Verdict = $"{areaConfig.DisplayName} is showing {activationText} activation with a {toneText} experience tone.",
            CurrentSituation = $"At present, this can show as: {TrimPeriod(situation)}.",
            AstrologicalReason = $"The main reason is that {topHouseText} are being activated while {dashaText} is running.",
            FutureChange = BuildFutureChange(area),
            ImportantPeriods = BuildImportantPeriods(snapshot.AnalysisDate, snapshot.Mahadasha, snapshot.Antardasha),
            ExpectedOutcome = BuildExpectedOutcome(area, supportText, pressureText),
            Evidence = BuildEvidence(topHouses, dashaText, supportText, pressureText)
        };
    }

    private static string BuildDashaText(Planet mahadasha, Planet antardasha)
    {
        return string.Equals(mahadasha.ToString(), antardasha.ToString(), StringComparison.OrdinalIgnoreCase)
            ? $"{mahadasha} Mahadasha"
            : $"{mahadasha} Mahadasha and {antardasha} Antardasha";
    }

    private static string BuildFutureChange(LifeAreaActivation area)
    {
        return area.Tone switch
        {
            "Supportive" => "The current activation can gradually become more useful when you act with consistency and avoid scattering effort.",
            "Pressured" => "The pressure is more likely to reduce through structure, patience and practical handling rather than sudden decisions.",
            _ => "The situation may keep alternating between support and pressure, so balanced decisions matter more than quick reactions."
        };
    }

    private static string BuildImportantPeriods(DateTimeOffset analysisDate, Planet mahadasha, Planet antardasha)
    {
        var month = analysisDate.ToString("MMMM yyyy");
        return $"This reading is anchored to {month} and the active {mahadasha}-{antardasha} Dasha context. Exact event timing is not included in this MVP analysis.";
    }

    private static string BuildExpectedOutcome(LifeAreaActivation area, string? supportText, string? pressureText)
    {
        if (area.Tone == "Supportive")
        {
            return supportText == null
                ? "The expected result is gradual improvement if the active houses are used through steady effort."
                : $"The expected result is constructive movement, especially because {LowerFirst(TrimPeriod(supportText))}.";
        }

        if (area.Tone == "Pressured")
        {
            return pressureText == null
                ? "The expected result is manageable progress after responsibility, delay or pressure is handled clearly."
                : $"The expected result can improve after careful handling, because {LowerFirst(TrimPeriod(pressureText))}.";
        }

        return "The expected result is mixed but workable: support is present, yet the pressure factors need practical attention before results feel stable.";
    }

    private static List<string> BuildEvidence(
        IReadOnlyList<FocusedActivatedHouseDto> houses,
        string dashaText,
        string? supportText,
        string? pressureText)
    {
        var evidence = new List<string> { $"Active Dasha context: {dashaText}." };
        evidence.AddRange(houses.Select(house => $"House {house.House} is active for {house.Role.ToLowerInvariant()} with score {house.ActivationScore}."));

        if (!string.IsNullOrWhiteSpace(supportText))
        {
            evidence.Add($"Support factor: {TrimPeriod(supportText)}.");
        }

        if (!string.IsNullOrWhiteSpace(pressureText))
        {
            evidence.Add($"Pressure factor: {TrimPeriod(pressureText)}.");
        }

        return evidence.Distinct(StringComparer.OrdinalIgnoreCase).Take(6).ToList();
    }

    private static string TrimPeriod(string value) => value.Trim().TrimEnd('.');

    private static string LowerFirst(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return value;
        }

        return char.ToLowerInvariant(value[0]) + value[1..];
    }
}
