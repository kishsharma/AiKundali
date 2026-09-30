using HoraLife.PredictionEngine;
using HoraLife.PredictionEngine.Enums;
using HoraLife.PredictionEngine.Models;

namespace HoraLife.PredictionEngine.Tests;

public sealed class EducationPredictionTests
{
    [Fact]
    public void Generate_WhenHigherHousesDominate_ReturnsHigherStudyPath()
    {
        var result = PredictionEngineFactory.CreateDefault().Generate(Request(), new PredictionContext
        {
            Houses =
            [
                House(5, "Sagittarius", "Jupiter"),
                House(9, "Aries", "Mars"),
                House(4, "Scorpio", "Mars")
            ],
            Planets =
            [
                Planet("Jupiter", 9, 80),
                Planet("Mars", 5, 40)
            ],
            DashaTimeline = EmptyDasha()
        });

        var path = Assert.Single(result.Claims, claim => claim.Key.StartsWith("education_path_", StringComparison.Ordinal));
        Assert.Equal("education_path_higher", path.Key);
        Assert.Equal(PredictionTone.Neutral, path.Tone);
        Assert.Contains("education_field_jupiter", result.Claims.Select(claim => claim.Key));
    }

    [Fact]
    public void Generate_WhenSkillHousesDominate_ReturnsSkillPath()
    {
        var result = PredictionEngineFactory.CreateDefault().Generate(Request(), new PredictionContext
        {
            Houses =
            [
                House(4, "Gemini", "Mercury"),
                House(5, "Cancer", "Moon")
            ],
            Planets =
            [
                Planet("Mercury", 10, 70),
                Planet("Moon", 4, 30)
            ],
            DashaTimeline = EmptyDasha()
        });

        var path = Assert.Single(result.Claims, claim => claim.Key.StartsWith("education_path_", StringComparison.Ordinal));
        Assert.Equal("education_path_skill", path.Key);
        Assert.Contains("education_field_mercury", result.Claims.Select(claim => claim.Key));
    }

    private static PredictionRequest Request()
    {
        return new PredictionRequest
        {
            Mode = PredictionMode.Report,
            LifeArea = PredictionLifeArea.Education,
            TimeScope = PredictionTimeScope.Current,
            ReferenceDate = DateTimeOffset.UtcNow
        };
    }

    private static HousePredictionData House(int number, string sign, string lord)
    {
        return new HousePredictionData
        {
            HouseNumber = number,
            Sign = sign,
            HouseLord = lord,
            Occupants = Array.Empty<string>(),
            AspectedBy = Array.Empty<string>(),
            DominantThemes = Array.Empty<string>()
        };
    }

    private static PlanetPredictionData Planet(string name, int house, int strength)
    {
        return new PlanetPredictionData
        {
            Planet = name,
            Sign = "Aries",
            HouseNumber = house,
            Longitude = house * 30,
            StrengthScore = strength,
            AspectedBy = Array.Empty<string>()
        };
    }

    private static DashaTimeline EmptyDasha()
    {
        return new DashaTimeline
        {
            Periods = Array.Empty<DashaPeriod>()
        };
    }
}
