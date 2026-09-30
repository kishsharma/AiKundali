using System.Text.Json;
using System.Text.RegularExpressions;
using KundaliEngine.API.DTOs.Responses;
using KundaliEngine.API.Services.ReportPdf.Html;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.FileProviders;

namespace HoraLife.PredictionEngine.Tests;

public class ReportHtmlHierarchyTests
{
    private static readonly string[] PaidSectionTitles =
    [
        "Contents",
        "At a glance",
        "Your life now",
        "Life chapters",
        "Kundali Charts",
        "Planet Positions & House Highlights",
        "Yogas & Doshas",
        "Mahadasha & Antardasha",
        "Remedies",
        "Panchang",
        "Rashi & Nakshatra Insights",
        "Kundali Scores",
        "12 Bhava Score",
        "Transit Insight",
        "Strength Summary",
        "Shadbala",
        "Ashtakavarga Support",
        "Sudarshan Chakra"
    ];

    [Fact]
    public void PaidModes_UseTheSameSectionOrder()
    {
        var service = CreateService();
        var profile = JsonSerializer.SerializeToElement(new { fullName = "Asha", birthDate = "1990-01-01", birthTime = "10:30", birthPlace = "Bidar" });
        var report = SampleReport();
        var chapters = new[]
        {
            new ReportLifeChapter
            {
                Title = "Career",
                Tone = "Supported",
                Summary = "Career looks supportive overall.",
                Window = "01 Jan 2027 - 01 Jun 2027"
            }
        };

        var desktop = SectionTitles(service.BuildDesktopColorHtml(profile, report, true, chapters));
        var mobile = SectionTitles(service.BuildMobileFriendlyHtml(profile, report, true, chapters));
        var print = SectionTitles(service.BuildPrintBlackWhiteHtml(profile, report, true, chapters));
        var corporate = SectionTitles(service.BuildCorporateHtml(profile, report, true, chapters));

        Assert.Equal(PaidSectionTitles, desktop);
        Assert.Equal(desktop, mobile);
        Assert.Equal(desktop, print);
        Assert.Equal(desktop, corporate);
        Assert.Contains("वक्रतुण्ड महाकाय", service.BuildDesktopColorHtml(profile, report, true, chapters));
    }

    [Fact]
    public void FreeModes_OmitLifeChaptersAndSudarshan()
    {
        var service = CreateService();
        var profile = JsonSerializer.SerializeToElement(new { fullName = "Asha", birthDate = "2012-01-01" });
        var report = SampleReport();

        foreach (var html in new[]
        {
            service.BuildDesktopColorHtml(profile, report, false, Array.Empty<ReportLifeChapter>()),
            service.BuildMobileFriendlyHtml(profile, report, false, Array.Empty<ReportLifeChapter>()),
            service.BuildPrintBlackWhiteHtml(profile, report, false, Array.Empty<ReportLifeChapter>()),
            service.BuildCorporateHtml(profile, report, false, Array.Empty<ReportLifeChapter>())
        })
        {
            var titles = SectionTitles(html);
            Assert.DoesNotContain("Life chapters", titles);
            Assert.DoesNotContain("Sudarshan Chakra", titles);
            Assert.DoesNotContain("Your life now", titles);
            Assert.Contains("Remedies", titles);
            Assert.Contains("Shadbala", titles);
            Assert.Contains("Kundali Scores", titles);
            Assert.Contains("Mahadasha & Antardasha", titles);
            Assert.Contains("वक्रतुण्ड महाकाय", html);
        }
    }

    private static ReportHtmlTemplateService CreateService()
    {
        return new ReportHtmlTemplateService(new TestHostEnvironment(FindApiRoot()));
    }

    private static GenerateKundaliResponse SampleReport()
    {
        return new GenerateKundaliResponse
        {
            Summary = new KundaliSummaryDto
            {
                Name = "Asha",
                Ascendant = "Leo",
                MoonSign = "Cancer",
                CurrentMahadasha = "Jupiter",
                CurrentAntardasha = "Saturn"
            },
            Panchang = new PanchangDto { Tithi = "Dwitiya", Yoga = "Siddhi", Karana = "Balava" },
            Planets = [new PlanetDto { Planet = "Sun", Sign = "Leo", House = 1, Nakshatra = "Magha" }],
            Houses =
            [
                new HouseDto { HouseNumber = 10, HouseName = "Karma", LifeAreas = ["Career"], Sign = "Taurus", Score = 80 },
                new HouseDto { HouseNumber = 6, HouseName = "Ripu", LifeAreas = ["Health"], Sign = "Capricorn", Score = 30 }
            ],
            Scores = new OverallScoreDto { Overall = 70, Stability = 60, Growth = 65, Challenge = 40 },
            CurrentDasha = new CurrentDashaDto
            {
                Mahadasha = "Jupiter",
                Antardasha = "Saturn",
                StartDate = new DateTime(2020, 1, 1),
                EndDate = new DateTime(2036, 1, 1),
                AntardashaStartDate = new DateTime(2026, 1, 1),
                AntardashaEndDate = new DateTime(2028, 6, 1),
                Summary = "A steady period for work."
            },
            MahadashaPeriods =
            [
                new MahadashaPeriodDto { Planet = "Saturn", Start = new DateTime(2018, 1, 1), End = new DateTime(2024, 1, 1) },
                new MahadashaPeriodDto { Planet = "Jupiter", Start = new DateTime(2024, 1, 1), End = new DateTime(2040, 1, 1) }
            ],
            RashiInsight = new RashiInsightDto { Name = "Simha", EnglishName = "Leo" },
            TransitInsight = new TransitInsightDto { Place = "Bidar", NatalAscendant = "Leo" },
            RemediesRecommendation = new RemediesRecommendationDto { Summary = "A steady practice supports the chart." },
            Yogas = [new YogaDto { Name = "Gaja Kesari" }],
            Doshas = [new DoshaDto { Name = "Mangal Dosha" }],
            SudarshanChakra = new SudarshanChakraDto { Purpose = "Overview", OverallPrediction = "Balanced emphasis." },
            AdvancedStrengthTables = new AdvancedStrengthTableDto
            {
                PlanetaryStrengths = [new PlanetaryStrengthRowDto { Planet = "Sun", TotalScore = 72, StrengthLevel = "Strong", SthanaBala = "High" }],
                Shadbala = [new ShadbalaRowDto { Planet = "Sun", Sign = "Leo", House = 1, Total = 8, Required = 6, Percent = 70, StrengthLevel = "Strong" }],
                HouseSupport = [new HouseSupportRowDto { House = 10, Sign = "Taurus", HouseName = "Karma", Score = 74, EstimatedBavPoints = 5, EstimatedSavPoints = 30, SupportLevel = "Strong", LifeAreas = "Career", TransitUsefulness = "Useful" }]
            }
        };
    }

    private static IReadOnlyList<string> SectionTitles(string html)
    {
        return Regex.Matches(html, "<h2 class=\"section-title\">(.*?)</h2>")
            .Select(match => System.Net.WebUtility.HtmlDecode(match.Groups[1].Value))
            .ToArray();
    }

    private static string FindApiRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            var direct = Path.Combine(dir.FullName, "Templates", "Reports", "kundali-report.html");
            if (File.Exists(direct))
            {
                return dir.FullName;
            }

            var nested = Path.Combine(dir.FullName, "Kundali portal", "KundaliEngine", "KundaliEngine.API", "Templates", "Reports", "kundali-report.html");
            if (File.Exists(nested))
            {
                return Path.GetDirectoryName(Path.GetDirectoryName(Path.GetDirectoryName(nested)))!;
            }

            dir = dir.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate the KundaliEngine.API report templates.");
    }

    private sealed class TestHostEnvironment : IWebHostEnvironment
    {
        public TestHostEnvironment(string contentRootPath)
        {
            ContentRootPath = contentRootPath;
            ContentRootFileProvider = new PhysicalFileProvider(contentRootPath);
        }

        public string ApplicationName { get; set; } = "tests";
        public IFileProvider ContentRootFileProvider { get; set; }
        public string ContentRootPath { get; set; }
        public string EnvironmentName { get; set; } = "Development";
        public string WebRootPath { get; set; } = string.Empty;
        public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();
    }
}
