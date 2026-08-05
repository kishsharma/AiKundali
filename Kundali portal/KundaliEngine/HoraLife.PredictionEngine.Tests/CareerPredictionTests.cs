using HoraLife.PredictionEngine.Enums;
using HoraLife.PredictionEngine.Models;
using HoraLife.PredictionEngine.Services;

namespace HoraLife.PredictionEngine.Tests;

public sealed class CareerPredictionTests
{
    [Theory]
    [InlineData( true, false,   "career_foundation_support", PredictionTone.Supportive)]
    [InlineData(false,true,"career_foundation_pressure",PredictionTone.Challenging)]
    [InlineData(true,true,"career_foundation_mixed",PredictionTone.Mixed)]
    public void Generate_WhenTenthHouseHasAspects_ReturnsExpectedCareerClaim(bool hasBeneficAspect, bool hasMaleficAspect, string expectedClaimKey,PredictionTone expectedTone)
    {
        // Arrange
        var request = CreateCareerRequest();

        var context = CreateContext(
            hasBeneficAspect,
            hasMaleficAspect);

        var service = new PredictionEngineService();

        // Act
        var result = service.Generate(request, context);

        // Assert
        var claim = Assert.Single(result.Claims);

        Assert.Equal(expectedClaimKey, claim.Key);
        Assert.Equal(expectedTone, claim.Tone);
        Assert.Equal(PredictionClaimStage.Natal, claim.Stage);

        Assert.Equal(PredictionLifeArea.Career, claim.LifeArea);

        Assert.Contains("house:10", claim.EvidenceKeys);
        AssertAspectEvidence(claim, hasBeneficAspect, hasMaleficAspect);
    }

    [Fact]
    public void Generate_WhenTenthHouseHasNoRelevantAspect_ReturnsNoCareerClaim()
    {
        // Arrange
        var request = CreateCareerRequest();

        var context = CreateContext(
            hasBeneficAspect: false,
            hasMaleficAspect: false);

        var service = new PredictionEngineService();

        // Act
        var result = service.Generate(request, context);

        // Assert
        Assert.Empty(result.Claims);
    }

    [Fact]
    public void Generate_WhenTenthHouseIsMissing_ReturnsNoCareerClaim()
    {
        // Arrange
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

        var service = new PredictionEngineService();

        // Act
        var result = service.Generate(request, context);

        // Assert
        Assert.Empty(result.Claims);
    }


    [Fact]
    public void Generate_WhenTenthHouseIsStrong_ReturnsSupportiveCareerClaim()
    {
        var request = CreateCareerRequest();

        var context = CreateContext(
            hasBeneficAspect: false,
            hasMaleficAspect: false,
            strengthLevel: "Strong");

        var service = new PredictionEngineService();

        var result = service.Generate(request, context);

        var claim = Assert.Single(result.Claims);

        Assert.Equal("career_foundation_support", claim.Key);
        Assert.Equal(PredictionTone.Supportive, claim.Tone);

        Assert.Contains(
            "career.10th_house.strong",
            claim.RuleIds);

        Assert.Contains(
            "house:10:strength:strong",
            claim.EvidenceKeys);
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

    private static PredictionContext CreateContext(bool hasBeneficAspect,bool hasMaleficAspect, string strengthLevel = "Neutral")
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
            Planets = Array.Empty<PlanetPredictionData>(),
            DashaTimeline = new DashaTimeline
            {
                Periods = Array.Empty<DashaPeriod>()
            }
        };
    }

    private static void AssertAspectEvidence( PredictionClaim claim, bool hasBeneficAspect, bool hasMaleficAspect)
    {
        if (hasBeneficAspect)
        {
            Assert.Contains(
                "career.10th_house.benefic_aspect",
                claim.RuleIds);

            Assert.Contains(
                "house:10:benefic_aspect",
                claim.EvidenceKeys);
        }
        else
        {
            Assert.DoesNotContain(
                "career.10th_house.benefic_aspect",
                claim.RuleIds);
        }

        if (hasMaleficAspect)
        {
            Assert.Contains(
                "career.10th_house.malefic_aspect",
                claim.RuleIds);

            Assert.Contains(
                "house:10:malefic_aspect",
                claim.EvidenceKeys);
        }
        else
        {
            Assert.DoesNotContain(
                "career.10th_house.malefic_aspect",
                claim.RuleIds);
        }
    }
}