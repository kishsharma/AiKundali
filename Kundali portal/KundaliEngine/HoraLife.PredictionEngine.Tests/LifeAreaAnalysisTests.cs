using KundaliEngine.API.DTOs.LifeArea;
using KundaliEngine.API.Services.LifeArea;
using KundaliEngine.Astrology.Services.DivisionalCharts;
using KundaliEngine.Domain.Enums;
using KundaliEngine.Domain.Models;
using KundaliEngine.KnowledgeBase.Services;

namespace HoraLife.PredictionEngine.Tests;

public class LifeAreaAnalysisTests
{
    [Fact]
    public void Metadata_LoadsSixteenChartsAndFifteenAreas()
    {
        var metadata = new DivisionalChartMetadataService();

        Assert.Equal(16, metadata.GetCharts().Count);
        Assert.Equal(15, metadata.GetAreas().Count);
        Assert.True(metadata.TryGetArea("career", out var career));
        Assert.Contains("D1", career.PrimaryCharts);
        Assert.Contains("D10", career.PrimaryCharts);
    }

    [Fact]
    public void CareerReport_ConsultsD1AndD10()
    {
        var service = new LifeAreaAnalysisService(
            new DivisionalChartMetadataService(),
            new AstrologyKnowledgeService(),
            new DivisionalChartService());

        var report = service.Analyze(CareerChart(), 5, "career", "en", null);

        Assert.Contains("D1", report.ChartsConsulted);
        Assert.Contains("D10", report.ChartsConsulted);
        Assert.Contains(report.Verdict, new[] { "supported", "mixed", "needsCare" });
        Assert.NotEmpty(report.Factors);
        Assert.NotEmpty(report.CrossChecks);
        Assert.All(report.CrossChecks, check =>
        {
            Assert.False(string.IsNullOrWhiteSpace(check.Planet));
            Assert.False(string.IsNullOrWhiteSpace(check.NatalSign));
            Assert.InRange(check.NatalHouse, 1, 12);
        });
        Assert.False(string.IsNullOrWhiteSpace(report.VerdictText));
        Assert.Contains("work you do", report.PlainMeaning);
        Assert.False(string.IsNullOrWhiteSpace(report.PlainMeaning));
    }

    [Fact]
    public void CareerReport_LabelsUpcomingMonthsByTone()
    {
        var service = new LifeAreaAnalysisService(
            new DivisionalChartMetadataService(),
            new AstrologyKnowledgeService(),
            new DivisionalChartService());

        var report = service.Analyze(CareerChart(), 5, "career", "en", new LifeAreaTiming
        {
            Mahadasha = "Saturn",
            Antardasha = "Mercury",
            Windows =
            [
                new LifeAreaWindow { Planet = nameof(Planet.Saturn), Start = new DateTime(2026, 3, 1), End = new DateTime(2026, 5, 1) },
                new LifeAreaWindow { Planet = nameof(Planet.Rahu), Start = new DateTime(2026, 5, 1), End = new DateTime(2026, 7, 1) }
            ]
        });

        Assert.Equal(2, report.Periods.Count);
        Assert.Contains(report.Periods[0].Tone, new[] { "support", "influence", "care" });
        Assert.Equal("care", report.Periods[1].Tone);
        Assert.Contains("2026", report.Periods[0].Label);
    }

    [Fact]
    public void DifficultReadingPrompt_KeepsVerdictLanguageAndConsultingTone()
    {
        var report = new LifeAreaReportDto
        {
            AreaName = "Career",
            Verdict = "needsCare",
            VerdictText = "The charts consulted for Career need care.",
            Factors =
            [
                new LifeAreaFactorDto
                {
                    Subject = "Saturn",
                    NatalSign = "Aries",
                    NatalDignity = "debilitated",
                    SpecialistChart = "D10",
                    SpecialistSign = "Cancer",
                    SpecialistDignity = "enemy sign",
                    Tone = "pressure",
                    Reading = "Saturn is under strain in the birth chart and in D10."
                }
            ],
            CrossValidation = "D1 and D10 differ.",
            Timing = "Saturn mahadasha connects with this area."
        };

        var message = LifeAreaInsightPrompt.BuildUserMessage(report, "en");

        Assert.Contains("needsCare", message);
        Assert.Contains("Saturn", message);
        Assert.Contains("Response language: English", message);
        Assert.Contains("Do not promise", message);
        Assert.Contains("Sound steady and consulting", message);
    }

    private static KundaliChart CareerChart()
    {
        return new KundaliChart
        {
            AscendantSign = ZodiacSign.Aries,
            AscendantDegree = 5,
            PlanetPositions =
            [
                Place(Planet.Sun, ZodiacSign.Aries, 10, HouseNumber.First),
                Place(Planet.Moon, ZodiacSign.Cancer, 6, HouseNumber.Fourth),
                Place(Planet.Mars, ZodiacSign.Scorpio, 8, HouseNumber.Eighth),
                Place(Planet.Mercury, ZodiacSign.Virgo, 12, HouseNumber.Sixth),
                Place(Planet.Jupiter, ZodiacSign.Sagittarius, 9, HouseNumber.Ninth),
                Place(Planet.Venus, ZodiacSign.Taurus, 4, HouseNumber.Second),
                Place(Planet.Saturn, ZodiacSign.Capricorn, 15, HouseNumber.Tenth),
                Place(Planet.Rahu, ZodiacSign.Gemini, 2, HouseNumber.Third),
                Place(Planet.Ketu, ZodiacSign.Sagittarius, 2, HouseNumber.Ninth)
            ]
        };
    }

    private static PlanetPosition Place(Planet planet, ZodiacSign sign, double degree, HouseNumber house)
    {
        return new PlanetPosition
        {
            Planet = planet,
            PlanetName = planet.ToString(),
            Sign = sign,
            DegreeInSign = degree,
            Degree = (int)degree,
            House = house,
            Longitude = ((int)sign - 1) * 30.0 + degree
        };
    }
}
