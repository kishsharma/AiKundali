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
}

