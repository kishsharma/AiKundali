using HoraLife.PredictionEngine;
using HoraLife.PredictionEngine.Enums;
using HoraLife.PredictionEngine.Models;
using KundaliEngine.API.DTOs.Responses;
using KundaliEngine.API.Prediction.Mappers;

namespace HoraLife.PredictionEngine.Tests;

public sealed class CustomQuestionPredictionTests
{
    [Fact]
    public void PredictionContextMapper_WhenD9Exists_MapsDivisionalChart()
    {
        var mapper = new PredictionContextMapper();
        var response = CreateGeneratedKundaliResponse();

        var context = mapper.Map(response);

        var d9 = Assert.Single(context.DivisionalCharts);
        Assert.Equal("D9", d9.ChartType);
        Assert.Equal("Libra", d9.AscendantSign);
        Assert.Equal("Aries", Assert.Single(d9.Houses.Where(h => h.HouseNumber == 7)).Sign);
    }

    [Fact]
    public void Generate_MarriagePossibilityWithSupportiveIndicators_ReturnsSupported()
    {
        var request = CreateQuestionRequest("marriage_possibility", PredictionTimeScope.Current);
        var context = CreateRelationshipsContext();
        var service = PredictionEngineFactory.CreateDefault();

        var result = service.Generate(request, context);

        var answer = result.Answer;
        Assert.NotNull(answer);
        Assert.Equal("Supported", answer.Outcome);
        Assert.Contains("relationships.primary_house.strong", answer.ReasonKeys);
        Assert.Contains("relationships.primary_house_lord.strong_dignity", answer.ReasonKeys);
        Assert.Contains("relationships.primary_house_lord.benefic_aspect", answer.ReasonKeys);
        Assert.Contains("house:7", answer.EvidenceKeys);
        Assert.Contains("planet:Mercury", answer.EvidenceKeys);
    }

    [Fact]
    public void Generate_MarriagePossibility_UsesD9ConfirmationAndResolvesDynamicLord()
    {
        var request = CreateQuestionRequest("marriage_possibility", PredictionTimeScope.Current);
        var context = CreateRelationshipsContext(divisionalCharts: [CreateD9Chart()]);
        var service = PredictionEngineFactory.CreateDefault();

        var result = service.Generate(request, context);

        var answer = result.Answer;
        Assert.NotNull(answer);
        Assert.Contains("relationships.d9.confirmation.supportive", answer.ReasonKeys);
        Assert.Contains("divisional:D9:house:7", answer.EvidenceKeys);
        Assert.Contains("divisional:D9:house:7:lord:Mars", answer.EvidenceKeys);
        Assert.Contains("divisional:D9:house:7:occupant:Ketu", answer.EvidenceKeys);
    }

    [Fact]
    public void Generate_MarriagePossibility_StrongFoundationWithMinorChallenges_ReturnsSupported()
    {
        var request = CreateQuestionRequest("marriage_possibility", PredictionTimeScope.Current);
        var context = CreateRelationshipsContext(
            primaryHouseHasMaleficAspect: true,
            supportiveSignificatorHasMaleficAspect: true,
            divisionalCharts: [CreateD9Chart()]);
        var service = PredictionEngineFactory.CreateDefault();

        var result = service.Generate(request, context);

        var answer = result.Answer;
        Assert.NotNull(answer);
        Assert.Equal("Supported", answer.Outcome);
        Assert.Contains("relationships.primary_house.malefic_aspect", answer.ReasonKeys);
        Assert.Contains("relationships.d9.confirmation.supportive", answer.ReasonKeys);
    }

    [Fact]
    public void Generate_MarriagePossibilityWithMixedPressure_ReturnsMixed()
    {
        var request = CreateQuestionRequest("marriage_possibility", PredictionTimeScope.Current);
        var context = CreateRelationshipsContext(
            primaryHouseHasMaleficAspect: true,
            primaryLordDetailedStrength: "Debilitated",
            primaryLordHasMaleficAspect: true,
            supportiveSignificatorHasMaleficAspect: true,
            primaryHouseStrength: 62);

        var service = PredictionEngineFactory.CreateDefault();

        var result = service.Generate(request, context);

        var answer = result.Answer;
        Assert.NotNull(answer);
        Assert.Equal("Mixed", answer.Outcome);
        Assert.Contains("relationships.primary_house.benefic_aspect", answer.ReasonKeys);
        Assert.Contains("relationships.primary_house.malefic_aspect", answer.ReasonKeys);
        Assert.Contains("relationships.primary_house_lord.weak_dignity", answer.ReasonKeys);
    }

    [Fact]
    public void Generate_MarriagePossibility_ChallengingFoundation_ReturnsChallenging()
    {
        var request = CreateQuestionRequest("marriage_possibility", PredictionTimeScope.Current);
        var context = CreateRelationshipsContext(
            primaryHouseHasMaleficAspect: true,
            primaryLordDetailedStrength: "Debilitated",
            primaryLordHasMaleficAspect: true,
            supportiveSignificatorHasMaleficAspect: true,
            primaryHouseStrength: 35,
            divisionalCharts: [CreateChallengingD9Chart()]);

        var service = PredictionEngineFactory.CreateDefault();

        var result = service.Generate(request, context);

        var answer = result.Answer;
        Assert.NotNull(answer);
        Assert.Equal("Challenging", answer.Outcome);
        Assert.Contains("relationships.primary_house.weak", answer.ReasonKeys);
        Assert.Contains("relationships.d9.confirmation.challenging", answer.ReasonKeys);
    }

    [Fact]
    public void Generate_MarriageTiming_ReturnsRankedWindows()
    {
        var request = CreateQuestionRequest(
            "marriage_timing",
            PredictionTimeScope.Future,
            new DateTimeOffset(2026, 12, 1, 0, 0, 0, TimeSpan.Zero));

        var context = CreateRelationshipsContext(
            dashaPeriods: CreateTimingPeriods(),
            divisionalCharts: [CreateD9Chart()]);

        var service = PredictionEngineFactory.CreateDefault();

        var result = service.Generate(request, context);

        var answer = result.Answer;
        Assert.NotNull(answer);
        Assert.NotEmpty(answer.TimingWindows);
        Assert.True(answer.TimingWindows.Count >= 4);
        Assert.Equal(new DateTimeOffset(2029, 7, 1, 0, 0, 0, TimeSpan.Zero), answer.TimingWindows[0].StartDate);
        Assert.Contains(answer.TimingWindows, window => window.StartDate == new DateTimeOffset(2027, 1, 1, 0, 0, 0, TimeSpan.Zero));
        Assert.Contains(answer.TimingWindows, window => window.StartDate == new DateTimeOffset(2028, 6, 1, 0, 0, 0, TimeSpan.Zero));
        Assert.Contains(answer.TimingWindows, window => window.StartDate == new DateTimeOffset(2030, 1, 1, 0, 0, 0, TimeSpan.Zero));
    }

    [Fact]
    public void Generate_MarriageTiming_IncludesD9TriggerEvidence()
    {
        var request = CreateQuestionRequest(
            "marriage_timing",
            PredictionTimeScope.Future,
            new DateTimeOffset(2026, 12, 1, 0, 0, 0, TimeSpan.Zero));

        var context = CreateRelationshipsContext(
            dashaPeriods: CreateTimingPeriods(),
            divisionalCharts: [CreateD9Chart()]);

        var service = PredictionEngineFactory.CreateDefault();

        var result = service.Generate(request, context);

        var answer = result.Answer;
        Assert.NotNull(answer);
        Assert.Contains(
            answer.TimingWindows.SelectMany(window => window.EvidenceKeys),
            evidenceKey => evidenceKey.StartsWith("divisional:D9:"));
    }

    [Fact]
    public void Generate_MarriageTiming_MultiActivatorWindowRanksAboveSingleWeakActivation()
    {
        var request = CreateQuestionRequest(
            "marriage_timing",
            PredictionTimeScope.Future,
            new DateTimeOffset(2026, 12, 1, 0, 0, 0, TimeSpan.Zero));

        var context = CreateRelationshipsContext(
            dashaPeriods: CreateTimingPeriods(),
            divisionalCharts: [CreateD9Chart()]);

        var service = PredictionEngineFactory.CreateDefault();

        var result = service.Generate(request, context);

        var answer = result.Answer;
        Assert.NotNull(answer);
        Assert.True(answer.TimingWindows.Count >= 2);

        var topWindow = answer.TimingWindows[0];
        var secondaryWindow = answer.TimingWindows.First(window => window.StartDate == new DateTimeOffset(2030, 1, 1, 0, 0, 0, TimeSpan.Zero));

        Assert.Equal(new DateTimeOffset(2029, 7, 1, 0, 0, 0, TimeSpan.Zero), topWindow.StartDate);
        Assert.Contains("Jupiter", topWindow.TriggerPlanets);
        Assert.Contains("Mercury", topWindow.TriggerPlanets);
        Assert.Equal("VeryStrong", topWindow.Strength);
        Assert.Equal("Strong", secondaryWindow.Strength);
        Assert.Contains("Jupiter", secondaryWindow.TriggerPlanets);
        Assert.Contains("Venus", secondaryWindow.TriggerPlanets);
    }

    [Fact]
    public void Generate_MarriageTiming_UsesConfiguredHorizonAndExcludesFarFutureWindows()
    {
        var request = CreateQuestionRequest(
            "marriage_timing",
            PredictionTimeScope.Future,
            new DateTimeOffset(2026, 12, 1, 0, 0, 0, TimeSpan.Zero));

        var context = CreateRelationshipsContext(
            dashaPeriods: CreateTimingPeriods(),
            divisionalCharts: [CreateD9Chart()]);

        var service = PredictionEngineFactory.CreateDefault();

        var result = service.Generate(request, context);

        var answer = result.Answer;
        Assert.NotNull(answer);
        Assert.DoesNotContain(answer.TimingWindows, window => window.StartDate >= new DateTimeOffset(2036, 12, 1, 0, 0, 0, TimeSpan.Zero));
        Assert.DoesNotContain(answer.TimingWindows, window => window.StartDate == new DateTimeOffset(2049, 2, 1, 0, 0, 0, TimeSpan.Zero));
    }

    [Fact]
    public void Generate_MarriageTiming_RanksEqualStrengthWindowsByProximity()
    {
        var request = CreateQuestionRequest(
            "marriage_timing",
            PredictionTimeScope.Future,
            new DateTimeOffset(2026, 12, 1, 0, 0, 0, TimeSpan.Zero));

        var context = CreateRelationshipsContext(
            dashaPeriods: CreateProximityTimingPeriods(),
            divisionalCharts: [CreateD9Chart()]);

        var service = PredictionEngineFactory.CreateDefault();

        var result = service.Generate(request, context);

        var answer = result.Answer;
        Assert.NotNull(answer);
        Assert.True(answer.TimingWindows.Count >= 2);
        Assert.Equal(new DateTimeOffset(2029, 7, 1, 0, 0, 0, TimeSpan.Zero), answer.TimingWindows[0].StartDate);
        Assert.Equal(new DateTimeOffset(2035, 8, 1, 0, 0, 0, TimeSpan.Zero), answer.TimingWindows[1].StartDate);
        Assert.Equal("VeryStrong", answer.TimingWindows[0].Strength);
        Assert.Equal("VeryStrong", answer.TimingWindows[1].Strength);
    }

    [Fact]
    public void Generate_MarriageTiming_ReturnsAtMostFiveAndDeduplicatesOverlappingSameMdAdWindows()
    {
        var request = CreateQuestionRequest(
            "marriage_timing",
            PredictionTimeScope.Future,
            new DateTimeOffset(2026, 12, 1, 0, 0, 0, TimeSpan.Zero));

        var context = CreateRelationshipsContext(
            dashaPeriods: CreateTimingPeriods(),
            divisionalCharts: [CreateD9Chart()]);

        var service = PredictionEngineFactory.CreateDefault();

        var result = service.Generate(request, context);

        var answer = result.Answer;
        Assert.NotNull(answer);
        Assert.True(answer.TimingWindows.Count <= 5);
        Assert.Equal(
            1,
            answer.TimingWindows.Count(window =>
                window.StartDate == new DateTimeOffset(2029, 7, 1, 0, 0, 0, TimeSpan.Zero) ||
                window.StartDate == new DateTimeOffset(2029, 8, 1, 0, 0, 0, TimeSpan.Zero)));
    }
    private static PredictionRequest CreateQuestionRequest(
        string focusKey,
        PredictionTimeScope timeScope,
        DateTimeOffset? referenceDate = null)
    {
        return new PredictionRequest
        {
            Mode = PredictionMode.CustomQuestion,
            LifeArea = PredictionLifeArea.Relationships,
            TimeScope = timeScope,
            FocusKey = focusKey,
            ReferenceDate = referenceDate ?? new DateTimeOffset(2026, 12, 1, 0, 0, 0, TimeSpan.Zero)
        };
    }

    private static PredictionContext CreateRelationshipsContext(
        bool primaryHouseHasMaleficAspect = false,
        string primaryLordDetailedStrength = "OwnSign",
        bool primaryLordHasMaleficAspect = false,
        bool supportiveSignificatorHasMaleficAspect = false,
        int primaryHouseStrength = 72,
        IReadOnlyList<DashaPeriod>? dashaPeriods = null,
        IReadOnlyList<DivisionalChartPredictionData>? divisionalCharts = null)
    {
        return new PredictionContext
        {
            Houses =
            [
                new HousePredictionData
                {
                    HouseNumber = 2,
                    Sign = "Cancer",
                    HouseLord = "Moon",
                    Occupants = Array.Empty<string>(),
                    AspectedBy = Array.Empty<string>(),
                    HasBeneficAspect = false,
                    HasMaleficAspect = false,
                    StrengthScore = 55,
                    StrengthLevel = "Average",
                    DominantThemes = ["Family"]
                },
                new HousePredictionData
                {
                    HouseNumber = 5,
                    Sign = "Leo",
                    HouseLord = "Sun",
                    Occupants = Array.Empty<string>(),
                    AspectedBy = Array.Empty<string>(),
                    HasBeneficAspect = false,
                    HasMaleficAspect = false,
                    StrengthScore = 55,
                    StrengthLevel = "Average",
                    DominantThemes = ["Romance"]
                },
                new HousePredictionData
                {
                    HouseNumber = 7,
                    Sign = "Gemini",
                    HouseLord = "Mercury",
                    Occupants = Array.Empty<string>(),
                    AspectedBy = ["Jupiter", "Saturn"],
                    HasBeneficAspect = true,
                    HasMaleficAspect = primaryHouseHasMaleficAspect,
                    StrengthScore = primaryHouseStrength,
                    StrengthLevel = "Strong",
                    DominantThemes = ["Marriage", "Partnership"]
                },
                new HousePredictionData
                {
                    HouseNumber = 11,
                    Sign = "Aries",
                    HouseLord = "Mars",
                    Occupants = Array.Empty<string>(),
                    AspectedBy = Array.Empty<string>(),
                    HasBeneficAspect = false,
                    HasMaleficAspect = false,
                    StrengthScore = 50,
                    StrengthLevel = "Average",
                    DominantThemes = ["Fulfillment"]
                }
            ],
            Planets =
            [
                new PlanetPredictionData
                {
                    Planet = "Mercury",
                    Sign = "Gemini",
                    HouseNumber = 7,
                    Longitude = 110,
                    Degree = 20,
                    Minute = 0,
                    Nakshatra = "Punarvasu",
                    Pada = 2,
                    IsCombust = false,
                    IsRetrograde = false,
                    AspectedBy = primaryLordHasMaleficAspect ? ["Saturn"] : ["Jupiter"],
                    HasBeneficAspect = true,
                    HasMaleficAspect = primaryLordHasMaleficAspect,
                    StrengthScore = 75,
                    StrengthLevel = "Strong",
                    DetailedStrength = primaryLordDetailedStrength,
                    ConditionTone = "Supportive",
                    FunctionalNature = "Benefic"
                },
                new PlanetPredictionData
                {
                    Planet = "Jupiter",
                    Sign = "Pisces",
                    HouseNumber = 4,
                    Longitude = 340,
                    Degree = 10,
                    Minute = 0,
                    Nakshatra = "Uttara Bhadrapada",
                    Pada = 1,
                    IsCombust = false,
                    IsRetrograde = false,
                    AspectedBy = Array.Empty<string>(),
                    HasBeneficAspect = true,
                    HasMaleficAspect = supportiveSignificatorHasMaleficAspect,
                    StrengthScore = 68,
                    StrengthLevel = "Strong",
                    DetailedStrength = "OwnSign",
                    ConditionTone = "Supportive",
                    FunctionalNature = "Benefic"
                },
                new PlanetPredictionData
                {
                    Planet = "Venus",
                    Sign = "Taurus",
                    HouseNumber = 6,
                    Longitude = 45,
                    Degree = 15,
                    Minute = 0,
                    Nakshatra = "Rohini",
                    Pada = 3,
                    IsCombust = false,
                    IsRetrograde = false,
                    AspectedBy = Array.Empty<string>(),
                    HasBeneficAspect = true,
                    HasMaleficAspect = false,
                    StrengthScore = 63,
                    StrengthLevel = "Strong",
                    DetailedStrength = "OwnSign",
                    ConditionTone = "Supportive",
                    FunctionalNature = "Benefic"
                },
                new PlanetPredictionData
                {
                    Planet = "Rahu",
                    Sign = "Leo",
                    HouseNumber = 5,
                    Longitude = 130,
                    Degree = 10,
                    Minute = 0,
                    Nakshatra = "Magha",
                    Pada = 1,
                    IsCombust = false,
                    IsRetrograde = false,
                    AspectedBy = Array.Empty<string>(),
                    HasBeneficAspect = false,
                    HasMaleficAspect = false,
                    StrengthScore = 50,
                    StrengthLevel = "Average",
                    DetailedStrength = "Neutral",
                    ConditionTone = "Neutral",
                    FunctionalNature = "Neutral"
                },
                new PlanetPredictionData
                {
                    Planet = "Moon",
                    Sign = "Cancer",
                    HouseNumber = 2,
                    Longitude = 90,
                    Degree = 10,
                    Minute = 0,
                    Nakshatra = "Pushya",
                    Pada = 1,
                    IsCombust = false,
                    IsRetrograde = false,
                    AspectedBy = Array.Empty<string>(),
                    HasBeneficAspect = false,
                    HasMaleficAspect = false,
                    StrengthScore = 52,
                    StrengthLevel = "Average",
                    DetailedStrength = "Neutral",
                    ConditionTone = "Neutral",
                    FunctionalNature = "Neutral"
                },
                new PlanetPredictionData
                {
                    Planet = "Mars",
                    Sign = "Aries",
                    HouseNumber = 11,
                    Longitude = 15,
                    Degree = 15,
                    Minute = 0,
                    Nakshatra = "Ashwini",
                    Pada = 2,
                    IsCombust = false,
                    IsRetrograde = false,
                    AspectedBy = Array.Empty<string>(),
                    HasBeneficAspect = false,
                    HasMaleficAspect = false,
                    StrengthScore = 58,
                    StrengthLevel = "Average",
                    DetailedStrength = "Neutral",
                    ConditionTone = "Neutral",
                    FunctionalNature = "Neutral"
                },
                new PlanetPredictionData
                {
                    Planet = "Ketu",
                    Sign = "Aquarius",
                    HouseNumber = 3,
                    Longitude = 300,
                    Degree = 12,
                    Minute = 0,
                    Nakshatra = "Dhanishta",
                    Pada = 4,
                    IsCombust = false,
                    IsRetrograde = false,
                    AspectedBy = Array.Empty<string>(),
                    HasBeneficAspect = false,
                    HasMaleficAspect = true,
                    StrengthScore = 45,
                    StrengthLevel = "Average",
                    DetailedStrength = "Neutral",
                    ConditionTone = "Challenging",
                    FunctionalNature = "Malefic"
                }
            ],
            DashaTimeline = new DashaTimeline
            {
                Periods = dashaPeriods ?? Array.Empty<DashaPeriod>()
            },
            DivisionalCharts = divisionalCharts ?? Array.Empty<DivisionalChartPredictionData>()
        };
    }

    private static DivisionalChartPredictionData CreateD9Chart()
    {
        return new DivisionalChartPredictionData
        {
            ChartType = "D9",
            AscendantSign = "Libra",
            Houses =
            [
                new DivisionalHousePredictionData
                {
                    HouseNumber = 7,
                    Sign = "Aries",
                    HouseLord = "Mars",
                    Occupants = ["Ketu"]
                }
            ],
            Planets =
            [
                new DivisionalPlanetPredictionData
                {
                    Planet = "Mars",
                    Sign = "Sagittarius",
                    HouseNumber = 3
                },
                new DivisionalPlanetPredictionData
                {
                    Planet = "Ketu",
                    Sign = "Aries",
                    HouseNumber = 7
                },
                new DivisionalPlanetPredictionData
                {
                    Planet = "Venus",
                    Sign = "Aries",
                    HouseNumber = 7
                }
            ]
        };
    }

    private static DivisionalChartPredictionData CreateChallengingD9Chart()
    {
        return new DivisionalChartPredictionData
        {
            ChartType = "D9",
            AscendantSign = "Libra",
            Houses =
            [
                new DivisionalHousePredictionData
                {
                    HouseNumber = 7,
                    Sign = "Aries",
                    HouseLord = "Mars",
                    Occupants = ["Rahu"]
                }
            ],
            Planets =
            [
                new DivisionalPlanetPredictionData
                {
                    Planet = "Mars",
                    Sign = "Taurus",
                    HouseNumber = 8
                },
                new DivisionalPlanetPredictionData
                {
                    Planet = "Rahu",
                    Sign = "Aries",
                    HouseNumber = 7
                },
                new DivisionalPlanetPredictionData
                {
                    Planet = "Venus",
                    Sign = "Virgo",
                    HouseNumber = 12
                }
            ]
        };
    }

    private static GenerateKundaliResponse CreateGeneratedKundaliResponse()
    {
        return new GenerateKundaliResponse
        {
            Planets = new List<PlanetDto>(),
            Houses = new List<HouseDto>(),
            MahadashaPeriods = new List<MahadashaPeriodDto>(),
            Yogas = new List<YogaDto>(),
            Doshas = new List<DoshaDto>(),
            D9Chart = new DivisionalChartDto
            {
                ChartType = "D9",
                Ascendant = "7 - Libra",
                Description = "Navamsa divisional chart",
                Houses =
                [
                    new HouseDto
                    {
                        HouseNumber = 7,
                        Sign = "Aries",
                        HouseOwner = "Mars",
                        Occupants = new List<string> { "Ketu" },
                        HouseName = "Seventh",
                        LifeAreas = new List<string>()
                    }
                ],
                Planets =
                [
                    new PlanetDto
                    {
                        Planet = "Mars",
                        Sign = "Sagittarius",
                        House = 3
                    },
                    new PlanetDto
                    {
                        Planet = "Ketu",
                        Sign = "Aries",
                        House = 7
                    },
                    new PlanetDto
                    {
                        Planet = "Venus",
                    Sign = "Aries",
                        House = 7
                    }
                ]
            }
        };
    }


    private static IReadOnlyList<DashaPeriod> CreateProximityTimingPeriods()
    {
        return
        [
            new DashaPeriod
            {
                PeriodId = "MD-Ketu-Near",
                Level = DashaLevel.Mahadasha,
                Planet = "Ketu",
                StartDate = new DateTimeOffset(2029, 1, 1, 0, 0, 0, TimeSpan.Zero),
                EndDate = new DateTimeOffset(2030, 1, 1, 0, 0, 0, TimeSpan.Zero)
            },
            new DashaPeriod
            {
                PeriodId = "AD-Jupiter-Near",
                ParentPeriodId = "MD-Ketu-Near",
                Level = DashaLevel.Antardasha,
                Planet = "Jupiter",
                StartDate = new DateTimeOffset(2029, 6, 1, 0, 0, 0, TimeSpan.Zero),
                EndDate = new DateTimeOffset(2029, 12, 1, 0, 0, 0, TimeSpan.Zero)
            },
            new DashaPeriod
            {
                PeriodId = "PD-Mercury-Near",
                ParentPeriodId = "AD-Jupiter-Near",
                Level = DashaLevel.Pratyantardasha,
                Planet = "Mercury",
                StartDate = new DateTimeOffset(2029, 7, 1, 0, 0, 0, TimeSpan.Zero),
                EndDate = new DateTimeOffset(2029, 11, 1, 0, 0, 0, TimeSpan.Zero)
            },
            new DashaPeriod
            {
                PeriodId = "MD-Ketu-Late",
                Level = DashaLevel.Mahadasha,
                Planet = "Ketu",
                StartDate = new DateTimeOffset(2035, 1, 1, 0, 0, 0, TimeSpan.Zero),
                EndDate = new DateTimeOffset(2036, 6, 1, 0, 0, 0, TimeSpan.Zero)
            },
            new DashaPeriod
            {
                PeriodId = "AD-Jupiter-Late",
                ParentPeriodId = "MD-Ketu-Late",
                Level = DashaLevel.Antardasha,
                Planet = "Jupiter",
                StartDate = new DateTimeOffset(2035, 6, 1, 0, 0, 0, TimeSpan.Zero),
                EndDate = new DateTimeOffset(2036, 2, 1, 0, 0, 0, TimeSpan.Zero)
            },
            new DashaPeriod
            {
                PeriodId = "PD-Mercury-Late",
                ParentPeriodId = "AD-Jupiter-Late",
                Level = DashaLevel.Pratyantardasha,
                Planet = "Mercury",
                StartDate = new DateTimeOffset(2035, 8, 1, 0, 0, 0, TimeSpan.Zero),
                EndDate = new DateTimeOffset(2035, 12, 1, 0, 0, 0, TimeSpan.Zero)
            }
        ];
    }
    private static IReadOnlyList<DashaPeriod> CreateTimingPeriods()
    {
        return
        [
            new DashaPeriod
            {
                PeriodId = "MD-Ketu-Early",
                Level = DashaLevel.Mahadasha,
                Planet = "Ketu",
                StartDate = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero),
                EndDate = new DateTimeOffset(2028, 6, 1, 0, 0, 0, TimeSpan.Zero)
            },
            new DashaPeriod
            {
                PeriodId = "AD-Saturn-2026",
                ParentPeriodId = "MD-Ketu-Early",
                Level = DashaLevel.Antardasha,
                Planet = "Saturn",
                StartDate = new DateTimeOffset(2026, 12, 1, 0, 0, 0, TimeSpan.Zero),
                EndDate = new DateTimeOffset(2027, 6, 1, 0, 0, 0, TimeSpan.Zero)
            },
            new DashaPeriod
            {
                PeriodId = "PD-Mercury-2027",
                ParentPeriodId = "AD-Saturn-2026",
                Level = DashaLevel.Pratyantardasha,
                Planet = "Mercury",
                StartDate = new DateTimeOffset(2027, 1, 1, 0, 0, 0, TimeSpan.Zero),
                EndDate = new DateTimeOffset(2027, 3, 1, 0, 0, 0, TimeSpan.Zero)
            },
            new DashaPeriod
            {
                PeriodId = "MD-Ketu-Mid",
                Level = DashaLevel.Mahadasha,
                Planet = "Ketu",
                StartDate = new DateTimeOffset(2028, 6, 1, 0, 0, 0, TimeSpan.Zero),
                EndDate = new DateTimeOffset(2032, 6, 1, 0, 0, 0, TimeSpan.Zero)
            },
            new DashaPeriod
            {
                PeriodId = "AD-Jupiter-2028",
                ParentPeriodId = "MD-Ketu-Mid",
                Level = DashaLevel.Antardasha,
                Planet = "Jupiter",
                StartDate = new DateTimeOffset(2028, 6, 1, 0, 0, 0, TimeSpan.Zero),
                EndDate = new DateTimeOffset(2029, 12, 1, 0, 0, 0, TimeSpan.Zero)
            },
            new DashaPeriod
            {
                PeriodId = "PD-Jupiter-2028",
                ParentPeriodId = "AD-Jupiter-2028",
                Level = DashaLevel.Pratyantardasha,
                Planet = "Jupiter",
                StartDate = new DateTimeOffset(2028, 6, 1, 0, 0, 0, TimeSpan.Zero),
                EndDate = new DateTimeOffset(2028, 10, 1, 0, 0, 0, TimeSpan.Zero)
            },
            new DashaPeriod
            {
                PeriodId = "PD-Mercury-2029-A",
                ParentPeriodId = "AD-Jupiter-2028",
                Level = DashaLevel.Pratyantardasha,
                Planet = "Mercury",
                StartDate = new DateTimeOffset(2029, 7, 1, 0, 0, 0, TimeSpan.Zero),
                EndDate = new DateTimeOffset(2029, 11, 1, 0, 0, 0, TimeSpan.Zero)
            },
            new DashaPeriod
            {
                PeriodId = "PD-Mercury-2029-B",
                ParentPeriodId = "AD-Jupiter-2028",
                Level = DashaLevel.Pratyantardasha,
                Planet = "Mercury",
                StartDate = new DateTimeOffset(2029, 8, 1, 0, 0, 0, TimeSpan.Zero),
                EndDate = new DateTimeOffset(2029, 10, 15, 0, 0, 0, TimeSpan.Zero)
            },
            new DashaPeriod
            {
                PeriodId = "AD-Venus-2030",
                ParentPeriodId = "MD-Ketu-Mid",
                Level = DashaLevel.Antardasha,
                Planet = "Venus",
                StartDate = new DateTimeOffset(2030, 1, 1, 0, 0, 0, TimeSpan.Zero),
                EndDate = new DateTimeOffset(2030, 8, 1, 0, 0, 0, TimeSpan.Zero)
            },
            new DashaPeriod
            {
                PeriodId = "PD-Jupiter-2030",
                ParentPeriodId = "AD-Venus-2030",
                Level = DashaLevel.Pratyantardasha,
                Planet = "Jupiter",
                StartDate = new DateTimeOffset(2030, 1, 1, 0, 0, 0, TimeSpan.Zero),
                EndDate = new DateTimeOffset(2030, 6, 1, 0, 0, 0, TimeSpan.Zero)
            },
            new DashaPeriod
            {
                PeriodId = "AD-Mercury-2031",
                ParentPeriodId = "MD-Ketu-Mid",
                Level = DashaLevel.Antardasha,
                Planet = "Mercury",
                StartDate = new DateTimeOffset(2031, 1, 1, 0, 0, 0, TimeSpan.Zero),
                EndDate = new DateTimeOffset(2031, 5, 1, 0, 0, 0, TimeSpan.Zero)
            },
            new DashaPeriod
            {
                PeriodId = "PD-Rahu-2031",
                ParentPeriodId = "AD-Mercury-2031",
                Level = DashaLevel.Pratyantardasha,
                Planet = "Rahu",
                StartDate = new DateTimeOffset(2031, 2, 1, 0, 0, 0, TimeSpan.Zero),
                EndDate = new DateTimeOffset(2031, 4, 1, 0, 0, 0, TimeSpan.Zero)
            },
            new DashaPeriod
            {
                PeriodId = "MD-Ketu-Late",
                Level = DashaLevel.Mahadasha,
                Planet = "Ketu",
                StartDate = new DateTimeOffset(2035, 1, 1, 0, 0, 0, TimeSpan.Zero),
                EndDate = new DateTimeOffset(2036, 6, 1, 0, 0, 0, TimeSpan.Zero)
            },
            new DashaPeriod
            {
                PeriodId = "AD-Jupiter-2035",
                ParentPeriodId = "MD-Ketu-Late",
                Level = DashaLevel.Antardasha,
                Planet = "Jupiter",
                StartDate = new DateTimeOffset(2035, 6, 1, 0, 0, 0, TimeSpan.Zero),
                EndDate = new DateTimeOffset(2036, 2, 1, 0, 0, 0, TimeSpan.Zero)
            },
            new DashaPeriod
            {
                PeriodId = "PD-Mercury-2035",
                ParentPeriodId = "AD-Jupiter-2035",
                Level = DashaLevel.Pratyantardasha,
                Planet = "Mercury",
                StartDate = new DateTimeOffset(2035, 8, 1, 0, 0, 0, TimeSpan.Zero),
                EndDate = new DateTimeOffset(2035, 12, 1, 0, 0, 0, TimeSpan.Zero)
            },
            new DashaPeriod
            {
                PeriodId = "MD-Ketu-Far",
                Level = DashaLevel.Mahadasha,
                Planet = "Ketu",
                StartDate = new DateTimeOffset(2048, 1, 1, 0, 0, 0, TimeSpan.Zero),
                EndDate = new DateTimeOffset(2050, 1, 1, 0, 0, 0, TimeSpan.Zero)
            },
            new DashaPeriod
            {
                PeriodId = "AD-Jupiter-2049",
                ParentPeriodId = "MD-Ketu-Far",
                Level = DashaLevel.Antardasha,
                Planet = "Jupiter",
                StartDate = new DateTimeOffset(2049, 1, 1, 0, 0, 0, TimeSpan.Zero),
                EndDate = new DateTimeOffset(2049, 6, 1, 0, 0, 0, TimeSpan.Zero)
            },
            new DashaPeriod
            {
                PeriodId = "PD-Mercury-2049",
                ParentPeriodId = "AD-Jupiter-2049",
                Level = DashaLevel.Pratyantardasha,
                Planet = "Mercury",
                StartDate = new DateTimeOffset(2049, 2, 1, 0, 0, 0, TimeSpan.Zero),
                EndDate = new DateTimeOffset(2049, 4, 1, 0, 0, 0, TimeSpan.Zero)
            }
        ];
    }
}








