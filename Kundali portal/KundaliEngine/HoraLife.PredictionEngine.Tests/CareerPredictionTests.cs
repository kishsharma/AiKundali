using HoraLife.PredictionEngine;
using HoraLife.PredictionEngine.Enums;
using HoraLife.PredictionEngine.Models;

namespace HoraLife.PredictionEngine.Tests;

public sealed class CareerPredictionTests
{
    [Theory]
    [InlineData(true, false, "career_foundation_support", PredictionTone.Supportive)]
    [InlineData(false, true, "career_foundation_pressure", PredictionTone.Challenging)]
    [InlineData(true, true, "career_foundation_mixed", PredictionTone.Mixed)]
    public void Generate_WhenTenthHouseHasAspects_ReturnsExpectedCareerClaim(
        bool hasBeneficAspect,
        bool hasMaleficAspect,
        string expectedClaimKey,
        PredictionTone expectedTone)
    {
        var request = CreateCareerRequest();
        var context = CreateContext(hasBeneficAspect, hasMaleficAspect);
        var service = PredictionEngineFactory.CreateDefault();

        var result = service.Generate(request, context);

        var claim = Assert.Single(result.Claims.Where(c => c.Key == expectedClaimKey && c.Stage == PredictionClaimStage.Natal));

        Assert.Equal(expectedTone, claim.Tone);
        Assert.Equal(PredictionLifeArea.Career, claim.LifeArea);
        Assert.Contains("house:10", claim.EvidenceKeys);
        AssertAspectEvidence(claim, hasBeneficAspect, hasMaleficAspect);
    }

    [Fact]
    public void Generate_WhenTenthHouseHasNoRelevantAspect_ReturnsNoCareerClaim()
    {
        var request = CreateCareerRequest();
        var context = CreateContext(
            hasBeneficAspect: false,
            hasMaleficAspect: false);

        var service = PredictionEngineFactory.CreateDefault();
        var result = service.Generate(request, context);

        Assert.Empty(result.Claims);
    }

    [Fact]
    public void Generate_WhenTenthHouseIsMissing_ReturnsNoCareerClaim()
    {
        var request = CreateCareerRequest();

        var context = new PredictionContext
        {
            Houses = Array.Empty<HousePredictionData>(),
            Planets = Array.Empty<PlanetPredictionData>(),
            DashaTimeline = new DashaTimeline
            {
                Periods = Array.Empty<DashaPeriod>()
            }
        };

        var service = PredictionEngineFactory.CreateDefault();
        var result = service.Generate(request, context);

        Assert.Empty(result.Claims);
    }

    [Fact]
    public void Generate_WhenTenthLordHasStrongDignity_ReturnsSupportiveCareerClaim()
    {
        var request = CreateCareerRequest();
        var context = CreateContext(
            hasBeneficAspect: false,
            hasMaleficAspect: false,
            includeTenthLordPlanet: true,
            tenthLordDetailedStrength: "OwnSign");

        var service = PredictionEngineFactory.CreateDefault();
        var result = service.Generate(request, context);

        var claim = Assert.Single(result.Claims.Where(c => c.Key == "career_10th_lord_dignity_support"));

        Assert.Equal(PredictionTone.Supportive, claim.Tone);
        Assert.Contains("career.10th_lord.strong_dignity", claim.RuleIds);
        Assert.Contains("planet:Saturn:dignity:OwnSign", claim.EvidenceKeys);
    }

    private static PredictionRequest CreateCareerRequest()
    {
        return new PredictionRequest
        {
            Mode = PredictionMode.Report,
            LifeArea = PredictionLifeArea.Career,
            TimeScope = PredictionTimeScope.Current,
            ReferenceDate = DateTimeOffset.UtcNow
        };
    }

    private static PredictionContext CreateContext(
        bool hasBeneficAspect,
        bool hasMaleficAspect,
        bool includeTenthLordPlanet = false,
        string tenthLordDetailedStrength = "Neutral")
    {
        var aspects = new List<string>();

        if (hasBeneficAspect)
        {
            aspects.Add("Jupiter");
        }

        if (hasMaleficAspect)
        {
            aspects.Add("Saturn");
        }

        var planets = includeTenthLordPlanet
            ? new[]
            {
                new PlanetPredictionData
                {
                    Planet = "Saturn",
                    Sign = "Capricorn",
                    HouseNumber = 10,
                    Longitude = 280,
                    Degree = 10,
                    Minute = 0,
                    Nakshatra = "Shravana",
                    Pada = 2,
                    IsCombust = false,
                    IsRetrograde = false,
                    AspectedBy = Array.Empty<string>(),
                    HasBeneficAspect = true,
                    HasMaleficAspect = false,
                    StrengthScore = 72,
                    StrengthLevel = "Strong",
                    DetailedStrength = tenthLordDetailedStrength,
                    ConditionTone = "Supportive",
                    FunctionalNature = "Neutral"
                }
            }
            : Array.Empty<PlanetPredictionData>();

        return new PredictionContext
        {
            Houses =
            [
                new HousePredictionData
                {
                    HouseNumber = 10,
                    Sign = "Capricorn",
                    HouseLord = "Saturn",
                    Occupants = Array.Empty<string>(),
                    AspectedBy = aspects,
                    HasBeneficAspect = hasBeneficAspect,
                    HasMaleficAspect = hasMaleficAspect,
                    StrengthScore = 70,
                    StrengthLevel = "Strong",
                    DominantThemes = ["Career", "Status"]
                }
            ],
            Planets = planets,
            DashaTimeline = new DashaTimeline
            {
                Periods = Array.Empty<DashaPeriod>()
            }
        };
    }

    private static void AssertAspectEvidence(PredictionClaim claim, bool hasBeneficAspect, bool hasMaleficAspect)
    {
        if (hasBeneficAspect)
        {
            Assert.Contains("career.10th_house.benefic_aspect", claim.RuleIds);
            Assert.Contains("house:10:benefic_aspect", claim.EvidenceKeys);
        }
        else
        {
            Assert.DoesNotContain("career.10th_house.benefic_aspect", claim.RuleIds);
        }

        if (hasMaleficAspect)
        {
            Assert.Contains("career.10th_house.malefic_aspect", claim.RuleIds);
            Assert.Contains("house:10:malefic_aspect", claim.EvidenceKeys);
        }
        else
        {
            Assert.DoesNotContain("career.10th_house.malefic_aspect", claim.RuleIds);
        }
    }

    [Fact]
    public void Generate_WhenServiceHousesDominate_ReturnsJobPathAndServiceFields()
    {
        var request = CreateCareerRequest();
        var context = new PredictionContext
        {
            Houses =
            [
                House(10, "Capricorn", "Saturn"),
                House(6, "Virgo", "Mercury")
            ],
            Planets =
            [
                Planet("Saturn", 6, 70),
                Planet("Sun", 10, 80),
                Planet("Mercury", 6, 40)
            ],
            DashaTimeline = EmptyDasha()
        };

        var result = PredictionEngineFactory.CreateDefault().Generate(request, context);

        var path = Assert.Single(result.Claims.Where(claim => claim.Key.StartsWith("career_path_", StringComparison.Ordinal)));
        Assert.Equal("career_path_job", path.Key);
        Assert.Equal(PredictionTone.Neutral, path.Tone);
        Assert.Contains("score:job:", path.EvidenceKeys.First(key => key.StartsWith("score:job:", StringComparison.Ordinal)));

        var fields = result.Claims
            .Where(claim => claim.Key.StartsWith("career_field_", StringComparison.Ordinal))
            .Select(claim => claim.Key)
            .ToArray();
        Assert.Contains("career_field_sun", fields);
        Assert.Contains("career_field_saturn", fields);
    }

    [Fact]
    public void Generate_WhenTradeHousesDominate_ReturnsBusinessPath()
    {
        var request = CreateCareerRequest();
        var context = new PredictionContext
        {
            Houses =
            [
                House(10, "Gemini", "Mercury"),
                House(7, "Pisces", "Jupiter")
            ],
            Planets =
            [
                Planet("Mercury", 7, 75),
                Planet("Venus", 10, 68),
                Planet("Jupiter", 10, 60)
            ],
            DashaTimeline = EmptyDasha()
        };

        var result = PredictionEngineFactory.CreateDefault().Generate(request, context);
        var path = Assert.Single(result.Claims.Where(claim => claim.Key.StartsWith("career_path_", StringComparison.Ordinal)));

        Assert.Equal("career_path_business", path.Key);
        Assert.Contains("career.field.karaka", result.Claims.Single(claim => claim.Key == "career_field_venus").RuleIds);
    }

    [Fact]
    public void Generate_WhenJobAndBusinessAreClose_ReturnsBothPaths()
    {
        var request = CreateCareerRequest();
        var context = new PredictionContext
        {
            Houses = [House(10, "Sagittarius", "Jupiter")],
            Planets = [Planet("Jupiter", 11, 55)],
            DashaTimeline = EmptyDasha()
        };

        var result = PredictionEngineFactory.CreateDefault().Generate(request, context);
        var path = Assert.Single(result.Claims.Where(claim => claim.Key.StartsWith("career_path_", StringComparison.Ordinal)));

        Assert.Equal("career_path_both", path.Key);
        Assert.Contains("career_field_jupiter", result.Claims.Select(claim => claim.Key));
    }

    [Fact]
    public void Generate_WhenDirectionClaimsExist_DoesNotChangeFoundationTone()
    {
        var request = CreateCareerRequest();
        var context = CreateContext(hasBeneficAspect: true, hasMaleficAspect: false, includeTenthLordPlanet: true, tenthLordDetailedStrength: "OwnSign");

        var result = PredictionEngineFactory.CreateDefault().Generate(request, context);
        var foundation = Assert.Single(result.Claims.Where(claim => claim.Key == "career_foundation_support"));
        var outlook = result.Claims.Single(claim => claim.Stage == PredictionClaimStage.Consolidated);

        Assert.Equal(PredictionTone.Supportive, foundation.Tone);
        Assert.Equal(PredictionTone.Supportive, outlook.Tone);
        Assert.Contains(result.Claims, claim => claim.Key == "career_path_job");
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

