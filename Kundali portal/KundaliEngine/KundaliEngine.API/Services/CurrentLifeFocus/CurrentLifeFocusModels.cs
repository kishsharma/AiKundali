using KundaliEngine.Domain.Enums;
using KundaliEngine.Domain.Models;

namespace KundaliEngine.API.Services.CurrentLifeFocus;

public class CurrentLifeFocusConfiguration
{
    public CurrentLifeFocusScoringConfiguration Scoring { get; set; } = new();

    public List<LifeAreaConfiguration> LifeAreas { get; set; } = new();
}

public class CurrentLifeFocusScoringConfiguration
{
    public double DashaActivationWeight { get; set; } = 0.5;

    public double GocharActivationWeight { get; set; } = 0.35;

    public double NatalSensitivityWeight { get; set; } = 0.15;

    public double MahadashaWeight { get; set; } = 0.4;

    public double AntardashaWeight { get; set; } = 0.6;

    public int HighActivationThreshold { get; set; } = 70;

    public int MediumActivationThreshold { get; set; } = 40;

    public int SupportiveToneThreshold { get; set; } = 12;

    public int PressuredToneThreshold { get; set; } = 12;

    public int AnalysisExpiryMinutes { get; set; } = 120;
}

public class LifeAreaConfiguration
{
    public string Key { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;

    public string SummaryTemplate { get; set; } = string.Empty;

    public List<LifeAreaHouseConfiguration> Houses { get; set; } = new();

    public List<string> Karakas { get; set; } = new();

    public List<string> PossibleExperiences { get; set; } = new();

    public List<string> SupportiveTemplates { get; set; } = new();

    public List<string> PressureTemplates { get; set; } = new();

    public string FollowUpQuestion { get; set; } = string.Empty;

    public List<LifeAreaOptionConfiguration> FollowUpOptions { get; set; } = new();
}

public class LifeAreaHouseConfiguration
{
    public int House { get; set; }

    public double Weight { get; set; } = 1.0;
}

public class LifeAreaOptionConfiguration
{
    public string Key { get; set; } = string.Empty;

    public string Label { get; set; } = string.Empty;
}

public class HouseActivationContext
{
    public IReadOnlyList<PlanetPosition> NatalPlanets { get; init; } = Array.Empty<PlanetPosition>();

    public IReadOnlyList<PlanetPosition> TransitPlanetsFromLagna { get; init; } = Array.Empty<PlanetPosition>();

    public IReadOnlyList<PlanetPosition> TransitPlanetsFromMoon { get; init; } = Array.Empty<PlanetPosition>();

    public AspectCalculationResult TransitAspectsFromLagna { get; init; } = new();

    public AspectCalculationResult TransitAspectsFromMoon { get; init; } = new();

    public IReadOnlyList<HouseScoreResult> NatalHouseScores { get; init; } = Array.Empty<HouseScoreResult>();

    public Planet Mahadasha { get; init; }

    public Planet Antardasha { get; init; }

    public ZodiacSign NatalAscendantSign { get; init; }

    public ZodiacSign NatalMoonSign { get; init; }

    public CurrentLifeFocusScoringConfiguration Scoring { get; init; } = new();
}

public class HouseActivationScore
{
    public int House { get; set; }

    public int DashaScore { get; set; }

    public int GocharScore { get; set; }

    public int NatalSensitivityScore { get; set; }

    public int TotalScore { get; set; }

    public int SupportScore { get; set; }

    public int PressureScore { get; set; }

    public List<string> SupportiveFactors { get; set; } = new();

    public List<string> PressureFactors { get; set; } = new();

    public List<string> Reasons { get; set; } = new();
}

public class LifeAreaActivation
{
    public string Key { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public int ActivationScore { get; set; }

    public string ActivationLevel { get; set; } = string.Empty;

    public string Tone { get; set; } = string.Empty;

    public int Confidence { get; set; }

    public int SupportScore { get; set; }

    public int PressureScore { get; set; }

    public List<int> ActivatedHouses { get; set; } = new();

    public List<string> ShortReasons { get; set; } = new();

    public List<string> SupportiveFactors { get; set; } = new();

    public List<string> PressureFactors { get; set; } = new();
}

public class CurrentLifeFocusSnapshot
{
    public Guid AnalysisId { get; set; }

    public Guid KundaliId { get; set; }

    public Guid? UserId { get; set; }

    public DateTimeOffset AnalysisDate { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public Planet Mahadasha { get; set; }

    public Planet Antardasha { get; set; }

    public List<HouseActivationScore> HouseScores { get; set; } = new();

    public List<LifeAreaActivation> Areas { get; set; } = new();
}
