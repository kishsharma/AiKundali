using System.Globalization;
using System.Text;
using System.Text.Json;
using KundaliEngine.API.DTOs.Requests;
using KundaliEngine.API.DTOs.Responses;
using KundaliEngine.API.Services.ReportPdf.Html;

namespace KundaliEngine.API.Services.ReportPdf;

public class ReportPdfService : IReportPdfService
{
    private readonly IReportHtmlTemplateService _htmlTemplateService;
    private readonly IHtmlReportPdfRenderer _htmlPdfRenderer;
    private readonly ReportLifeChapterBuilder _lifeChapterBuilder;
    private readonly ReportAdditionsBuilder _additionsBuilder;
    private readonly ILogger<ReportPdfService> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public ReportPdfService(
        IReportHtmlTemplateService htmlTemplateService,
        IHtmlReportPdfRenderer htmlPdfRenderer,
        ReportLifeChapterBuilder lifeChapterBuilder,
        ReportAdditionsBuilder additionsBuilder,
        ILogger<ReportPdfService> logger)
    {
        _htmlTemplateService = htmlTemplateService;
        _htmlPdfRenderer = htmlPdfRenderer;
        _lifeChapterBuilder = lifeChapterBuilder;
        _additionsBuilder = additionsBuilder;
        _logger = logger;
    }

    public byte[] GenerateReportPdf(ReportPdfRequest request)
    {
        if (request.BirthProfile.ValueKind is JsonValueKind.Undefined or JsonValueKind.Null)
        {
            throw new InvalidOperationException("Birth profile is required to generate report PDF.");
        }

        if (request.Report.ValueKind is JsonValueKind.Undefined or JsonValueKind.Null)
        {
            throw new InvalidOperationException("Report data is required to generate report PDF.");
        }

        var report = request.Report.Deserialize<GenerateKundaliResponse>(JsonOptions)
            ?? throw new InvalidOperationException("Report data is not valid.");

        var includePremium = string.Equals(request.Audience, "paid", StringComparison.OrdinalIgnoreCase);
        if (!includePremium)
        {
            ApplyFreeReport(report);
        }

        IReadOnlyList<Html.ReportLifeChapter> chapters = Array.Empty<Html.ReportLifeChapter>();
        var additions = new ReportPdfAdditions();
        if (includePremium)
        {
            try
            {
                chapters = _lifeChapterBuilder.Build(request.BirthProfile, report);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Life chapter reading failed. The report will continue without life chapters.");
            }

            try
            {
                additions = new ReportPdfAdditions
                {
                    Personality = _additionsBuilder.BuildPersonality(report),
                    LifeAreaReadings = _additionsBuilder.BuildLifeAreaReadings(report),
                    TwoYearForecast = _additionsBuilder.BuildTwoYearForecast(request.BirthProfile, report)
                };
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Additional paid report sections failed. The report will continue without them.");
            }
        }

        if (ShouldUseCorporateHtml(request.Mode))
        {
            try
            {
                var html = _htmlTemplateService.BuildCorporateHtml(request.BirthProfile, report, includePremium, chapters, additions);
                return _htmlPdfRenderer.RenderPdf(html, BuildFileName(request));
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Corporate advisory HTML report rendering failed. Falling back to manual PDF renderer.");
            }
        }

        if (ShouldUsePrintBlackWhiteHtml(request.Mode))
        {
            try
            {
                var html = _htmlTemplateService.BuildPrintBlackWhiteHtml(request.BirthProfile, report, includePremium, chapters, additions);
                return _htmlPdfRenderer.RenderPdf(html, BuildFileName(request));
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Black-and-white HTML report rendering failed. Falling back to manual PDF renderer.");
            }
        }

        if (ShouldUseMobileHtml(request.Mode))
        {
            try
            {
                var html = _htmlTemplateService.BuildMobileFriendlyHtml(request.BirthProfile, report, includePremium, chapters, additions);
                return _htmlPdfRenderer.RenderPdf(html, BuildFileName(request));
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Mobile-friendly HTML report rendering failed. Falling back to manual PDF renderer.");
            }
        }

        if (ShouldUseDesktopColorHtml(request.Mode))
        {
            try
            {
                var html = _htmlTemplateService.BuildDesktopColorHtml(request.BirthProfile, report, includePremium, chapters, additions);
                return _htmlPdfRenderer.RenderPdf(html, BuildFileName(request));
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Desktop-color HTML report rendering failed. Falling back to manual PDF renderer.");
            }
        }

        var document = BuildDocument(request.BirthProfile, report, additions);
        return StyledPdfWriter.Write(document);
    }

    public string BuildFileName(ReportPdfRequest request)
    {
        if (!string.IsNullOrWhiteSpace(request.FileName))
        {
            return SanitizeFileName(request.FileName!);
        }

        var name = BuildPersonDisplayName(request.BirthProfile, GetString(request.BirthProfile, "fullName", "name"));
        var safeName = string.IsNullOrWhiteSpace(name) ? "kundali" : name;
        return SanitizeFileName($"{safeName}-kundali-report.pdf");
    }

    private static void ApplyFreeReport(GenerateKundaliResponse report)
    {
        report.SudarshanChakra = null;
        report.Predictions = new();

        foreach (var yoga in report.Yogas ?? new List<YogaDto>())
        {
            yoga.Impact = string.Empty;
            yoga.ImpactAreas = new();
            yoga.ActivationPeriods = null;
        }

        foreach (var dosha in report.Doshas ?? new List<DoshaDto>())
        {
            dosha.ImpactAreas = new();
            dosha.ActivationPeriods = null;
            dosha.Remedies = new();
        }
    }

    private static bool ShouldUseCorporateHtml(string? mode)
    {
        return string.Equals(mode, "corporate", StringComparison.OrdinalIgnoreCase)
            || string.Equals(mode, "corporate-advisory", StringComparison.OrdinalIgnoreCase);
    }
    private static bool ShouldUsePrintBlackWhiteHtml(string? mode)
    {
        return string.Equals(mode, "print-bw", StringComparison.OrdinalIgnoreCase)
            || string.Equals(mode, "black-white", StringComparison.OrdinalIgnoreCase)
            || string.Equals(mode, "black-and-white", StringComparison.OrdinalIgnoreCase);
    }

    private static bool ShouldUseMobileHtml(string? mode)
    {
        return string.Equals(mode, "mobile", StringComparison.OrdinalIgnoreCase)
            || string.Equals(mode, "mobile-friendly", StringComparison.OrdinalIgnoreCase)
            || string.Equals(mode, "mobilefriendly", StringComparison.OrdinalIgnoreCase);
    }

    private static bool ShouldUseDesktopColorHtml(string? mode)
    {
        return string.Equals(mode, "desktop", StringComparison.OrdinalIgnoreCase)
            || string.Equals(mode, "desktop-color", StringComparison.OrdinalIgnoreCase)
            || string.Equals(mode, "desktopfriendly-color-theme", StringComparison.OrdinalIgnoreCase);
    }

    private static PdfDocument BuildDocument(JsonElement birthProfile, GenerateKundaliResponse report, ReportPdfAdditions additions)
    {
        var name = ValueOrDash(BuildPersonDisplayName(birthProfile, report.Summary?.Name));
        var birthDate = ValueOrDash(GetString(birthProfile, "birthDate", "dateOfBirth"));
        var birthTime = ValueOrDash(GetString(birthProfile, "birthTime", "timeOfBirth"));
        var birthPlace = ValueOrDash(GetString(birthProfile, "birthPlace", "placeOfBirth", "birthplace"));
        var summary = report.Summary;
        var familyCells = BuildFamilyProfileCells(birthProfile);

        var branding = GetObject(birthProfile, "branding", "whiteLabel", "astrologerBranding");
        var hasBranding = branding.ValueKind == JsonValueKind.Object;
        var preparedBy = hasBranding
            ? FirstNonEmpty(GetString(branding, "astrologerName", "name"), "Pandit Vasudev Sharma")
            : "Pandit Vasudev Sharma";
        var preparedDetail = hasBranding
            ? FirstNonEmpty(GetString(branding, "address"), GetString(branding, "contactNumber", "phone", "mobile"), GetString(branding, "email"))
            : "HoraLife";

        var lines = new List<PdfLine>
        {
            PdfLine.CoverTitle("HoraLife Kundali Report"),
            PdfLine.CoverSubTitle($"Prepared for {name}"),
            PdfLine.Meta($"Generated {DateTime.Now:dd MMM yyyy, hh:mm tt}"),
            PdfLine.Grid("Prepared By", preparedBy, hasBranding ? "Contact" : "Studio", preparedDetail),
            PdfLine.Space(8),
            PdfLine.Section("Birth Profile"),
            PdfLine.Grid("Name", name, "Date", birthDate),
            PdfLine.Grid("Time", birthTime, "Place", birthPlace)
        };

        if (familyCells.Length > 0)
        {
            lines.Add(PdfLine.Grid(familyCells));
        }

        AddYogasDoshasSections(lines, report);

        lines.Add(PdfLine.Space(5));
        lines.Add(PdfLine.Section("Core Identity"));

        if (summary is not null)
        {
            lines.Add(PdfLine.Grid("Ascendant", ValueOrDash(summary.Ascendant), "Moon Sign", ValueOrDash(summary.MoonSign), "Sun Sign", ValueOrDash(summary.SunSign)));
            lines.Add(PdfLine.Grid("Nakshatra", ValueOrDash(summary.Nakshatra), "Current Dasha", $"{ValueOrDash(summary.CurrentMahadasha)} / {ValueOrDash(summary.CurrentAntardasha)}"));
            AddParagraph(lines, summary.Summary, 1100);
        }

        if (report.Panchang is not null)
        {
            lines.Add(PdfLine.Space(7));
            lines.Add(PdfLine.Section("Panchang"));
            lines.Add(PdfLine.Grid("Tithi", $"{ValueOrDash(report.Panchang.Tithi)} ({ValueOrDash(report.Panchang.Paksha)})", "Nakshatra", $"{ValueOrDash(report.Panchang.Nakshatra)} Pada {report.Panchang.Pada}"));
            lines.Add(PdfLine.Grid("Yoga", ValueOrDash(report.Panchang.Yoga), "Karana", ValueOrDash(report.Panchang.Karana), "Vaar", ValueOrDash(report.Panchang.Vaar)));
            AddParagraph(lines, report.Panchang.Summary, 700);
        }

        AddChartVisualSections(lines, report);
        AddScoresSection(lines, report.Scores);
        AddCurrentDashaSection(lines, report.CurrentDasha);

        if (report.Planets?.Count > 0)
        {
            lines.Add(PdfLine.Space(7));
            lines.Add(PdfLine.Section("Planet Positions"));
            lines.Add(PdfLine.TableHeader("Planet", "Sign", "Degree", "House", "Nakshatra", "Strength"));
            foreach (var planet in report.Planets.Take(18))
            {
                lines.Add(PdfLine.TableRow(
                    PlanetIcon(planet.Planet) + " " + planet.Planet,
                    planet.Sign,
                    $"{planet.Degree}d {planet.Minute:00}m",
                    $"H{planet.House}",
                    ValueOrDash(planet.Nakshatra),
                    PlanetStrength(planet)));
            }
        }

        if (report.Houses?.Count > 0)
        {
            lines.Add(PdfLine.Space(7));
            lines.Add(PdfLine.Section("House Highlights"));
            lines.Add(PdfLine.TableHeader("House", "Sign", "Owner", "Occupants", "Score"));
            foreach (var house in report.Houses.OrderBy(h => h.HouseNumber).Take(12))
            {
                var occupants = house.Occupants?.Count > 0 ? string.Join(", ", house.Occupants.Take(4)) : "None";
                lines.Add(PdfLine.TableRow(
                    house.HouseNumber.ToString(CultureInfo.InvariantCulture),
                    ValueOrDash(house.Sign),
                    ValueOrDash(string.IsNullOrWhiteSpace(house.HouseOwner) ? house.HouseSwamy : house.HouseOwner),
                    occupants,
                    house.Score.ToString(CultureInfo.InvariantCulture)));
            }
        }

        AddMahadashaTimelineSection(lines, report.MahadashaPeriods);
        AddSudarshanSection(lines, report.SudarshanChakra);
        AddRemediesSection(lines, report.RemediesRecommendation);
        AddTransitSection(lines, report.TransitInsight);
        AddAdditionsSections(lines, additions);

        return new PdfDocument(
            "HoraLife",
            "Kundali Report",
            $"{name} | {birthDate} | {birthPlace}",
            lines);
    }

    private static void AddYogasDoshasSections(List<PdfLine> lines, GenerateKundaliResponse report)
    {
        if (report.Yogas?.Count > 0)
        {
            lines.Add(PdfLine.Space(5));
            lines.Add(PdfLine.Section("Yogas"));
            foreach (var yoga in report.Yogas.Take(8))
            {
                lines.Add(PdfLine.Bullet(ValueOrDash(GetProperty(yoga, "Name", "YogaName", "name", "yogaName")), ValueOrDash(GetProperty(yoga, "Description", "Result", "Summary", "description", "result", "summary"))));
            }
        }

        if (report.Doshas?.Count > 0)
        {
            lines.Add(PdfLine.Space(5));
            lines.Add(PdfLine.Section("Doshas"));
            foreach (var dosha in report.Doshas.Take(8))
            {
                lines.Add(PdfLine.Bullet(ValueOrDash(GetProperty(dosha, "Name", "DoshaName", "name", "doshaName")), ValueOrDash(GetProperty(dosha, "Description", "Result", "Summary", "description", "result", "summary"))));
            }
        }
    }

    private static void AddAdditionsSections(List<PdfLine> lines, ReportPdfAdditions additions)
    {
        if (additions.Personality is not null)
        {
            lines.Add(PdfLine.Space(7));
            lines.Add(PdfLine.Section("Personality Profile"));
            AddParagraph(lines, additions.Personality.CorePersonality, 500);
            AddParagraph(lines, additions.Personality.EmotionalPattern, 500);
            lines.Add(PdfLine.Bullet("Natural strengths", string.Join("; ", additions.Personality.NaturalStrengths)));
            lines.Add(PdfLine.Bullet("Awareness areas", string.Join("; ", additions.Personality.AwarenessAreas)));
            AddParagraph(lines, additions.Personality.PressureResponse, 500);
            AddParagraph(lines, additions.Personality.GrowthGuidance, 500);
        }

        if (additions.LifeAreaReadings.Count > 0)
        {
            lines.Add(PdfLine.Space(7));
            lines.Add(PdfLine.Section("Life Area Readings"));
            foreach (var reading in additions.LifeAreaReadings)
            {
                lines.Add(PdfLine.Bullet($"{reading.LifeArea} ({reading.Tone})", reading.Summary));
                if (reading.Windows.Count > 0) lines.Add(PdfLine.Bullet("Timing", string.Join(", ", reading.Windows)));
            }
        }

        if (additions.TwoYearForecast.Count > 0)
        {
            lines.Add(PdfLine.Space(7));
            lines.Add(PdfLine.Section("Two-Year Outlook"));
            foreach (var forecast in additions.TwoYearForecast)
            {
                lines.Add(PdfLine.Bullet($"{forecast.LifeArea} ({forecast.Tone})", forecast.Summary));
                lines.Add(PdfLine.Bullet("Windows", string.Join(", ", forecast.Windows)));
            }
        }
    }

    private static void AddChartVisualSections(List<PdfLine> lines, GenerateKundaliResponse report)
    {
        var hasD1 = report.Planets?.Count > 0;
        var hasD9 = report.D9Chart?.Planets?.Count > 0;
        var hasGochar = report.TransitInsight?.Planets?.Count > 0 || report.TransitInsight?.Activations?.Count > 0;
        var hasSudarshan = report.SudarshanChakra?.HouseEmphasis?.Count > 0;

        if (!hasD1 && !hasD9 && !hasGochar && !hasSudarshan)
        {
            return;
        }

        lines.Add(PdfLine.Space(7));
        lines.Add(PdfLine.Section("Kundali Charts"));

        if (hasD1)
        {
            lines.Add(PdfLine.ChartVisual("D1 Lagna Kundali", "Natal house placement", BuildPlanetHouseCells(report.Planets)));
        }

        if (hasD9)
        {
            lines.Add(PdfLine.ChartVisual("D9 Navamsa Kundali", FirstNonEmpty(report.D9Chart?.Description, report.D9Chart?.Ascendant), BuildPlanetHouseCells(report.D9Chart!.Planets)));
        }

        if (hasGochar)
        {
            lines.Add(PdfLine.ChartVisual("Gochar Kundali", BuildGocharNote(report.TransitInsight), BuildGocharHouseCells(report.TransitInsight)));
        }

        if (hasSudarshan)
        {
            lines.Add(PdfLine.ChartVisual("Sudarshan Chakra", $"Overall score {report.SudarshanChakra!.OverallScore}", BuildSudarshanHouseCells(report.SudarshanChakra)));
        }
    }

    private static string[] BuildPlanetHouseCells(IEnumerable<PlanetDto>? planets)
    {
        var houses = EmptyHouseCells();
        if (planets is null)
        {
            return houses;
        }

        foreach (var group in planets.Where(p => p.House >= 1 && p.House <= 12).GroupBy(p => p.House))
        {
            houses[group.Key - 1] = string.Join(" ", group.Take(5).Select(PlanetShortName));
        }

        return houses;
    }

    private static string[] BuildGocharHouseCells(TransitInsightDto? transit)
    {
        var houses = BuildPlanetHouseCells(transit?.Planets);
        if (houses.Any(cell => cell != "-"))
        {
            return houses;
        }

        if (transit?.Activations is null)
        {
            return houses;
        }

        foreach (var group in transit.Activations.Where(a => a.NatalHouse >= 1 && a.NatalHouse <= 12).GroupBy(a => a.NatalHouse))
        {
            houses[group.Key - 1] = string.Join(" ", group.Take(5).Select(a => PlanetShortName(a.Planet)));
        }

        return houses;
    }

    private static string[] BuildSudarshanHouseCells(SudarshanChakraDto? sudarshan)
    {
        var houses = EmptyHouseCells();
        if (sudarshan?.HouseEmphasis is null)
        {
            return houses;
        }

        foreach (var item in sudarshan.HouseEmphasis.Where(h => h.HouseNumber >= 1 && h.HouseNumber <= 12))
        {
            var repeated = item.IsRepeatedAcrossReferences ? "*" : string.Empty;
            houses[item.HouseNumber - 1] = $"T{item.TotalPlanetCount}{repeated} L{item.LagnaPlanetCount} M{item.MoonPlanetCount} S{item.SunPlanetCount}";
        }

        return houses;
    }

    private static string BuildGocharNote(TransitInsightDto? transit)
    {
        if (transit is null)
        {
            return "Current transit placements";
        }

        var date = transit.CalculatedAtUtc == default ? string.Empty : transit.CalculatedAtUtc.ToString("dd MMM yyyy", CultureInfo.InvariantCulture);
        return FirstNonEmpty($"{date} {transit.Place}".Trim(), transit.NatalAscendant, "Current transit placements");
    }

    private static string[] EmptyHouseCells() => Enumerable.Repeat("-", 12).ToArray();

    private static string PlanetShortName(PlanetDto planet) => PlanetShortName(planet.Planet) + (planet.IsRetrograde ? "R" : string.Empty);

    private static string PlanetShortName(string? planet) => (planet ?? string.Empty).Trim().ToLowerInvariant() switch
    {
        "sun" => "Su",
        "moon" => "Mo",
        "mars" => "Ma",
        "mercury" => "Me",
        "jupiter" => "Ju",
        "venus" => "Ve",
        "saturn" => "Sa",
        "rahu" => "Ra",
        "ketu" => "Ke",
        _ => string.IsNullOrWhiteSpace(planet) ? "-" : planet.Trim()[..Math.Min(2, planet.Trim().Length)]
    };
    private static void AddScoresSection(List<PdfLine> lines, OverallScoreDto? scores)
    {
        if (scores is null)
        {
            return;
        }

        lines.Add(PdfLine.Space(7));
        lines.Add(PdfLine.Section("Kundali Scores"));
        lines.Add(PdfLine.TableHeader("Area", "Score", "Meaning", "Key Factors"));
        lines.Add(PdfLine.TableRow("Overall", scores.Overall.ToString(CultureInfo.InvariantCulture), ScoreBand(scores.Overall), JoinCompact(scores.OverallFactors, scores.Factors, 110)));
        lines.Add(PdfLine.TableRow("Stability", scores.Stability.ToString(CultureInfo.InvariantCulture), ScoreBand(scores.Stability), JoinCompact(scores.StabilityFactors, null, 110)));
        lines.Add(PdfLine.TableRow("Growth", scores.Growth.ToString(CultureInfo.InvariantCulture), ScoreBand(scores.Growth), JoinCompact(scores.GrowthFactors, null, 110)));
        lines.Add(PdfLine.TableRow("Challenge", scores.Challenge.ToString(CultureInfo.InvariantCulture), ScoreBand(scores.Challenge, true), JoinCompact(scores.ChallengeFactors, null, 110)));

        if (scores.DominantStrengths.Count > 0 || scores.DominantWeaknesses.Count > 0)
        {
            lines.Add(PdfLine.KeyValue("Dominant Strengths", JoinCompact(scores.DominantStrengths, null, 120)));
            lines.Add(PdfLine.KeyValue("Dominant Weaknesses", JoinCompact(scores.DominantWeaknesses, null, 120)));
        }

        AddScoreBreakdown(lines, "Overall Breakdown", scores.ScoreBreakdown.Overall);
        AddScoreBreakdown(lines, "Stability Breakdown", scores.ScoreBreakdown.Stability);
        AddScoreBreakdown(lines, "Growth Breakdown", scores.ScoreBreakdown.Growth);
        AddScoreBreakdown(lines, "Challenge Breakdown", scores.ScoreBreakdown.Challenge);
    }

    private static void AddScoreBreakdown(List<PdfLine> lines, string title, List<ScoreComponentDto>? components)
    {
        if (components is null || components.Count == 0)
        {
            return;
        }

        lines.Add(PdfLine.Space(4));
        lines.Add(PdfLine.KeyValue(title, "Score | Weight | Contribution"));
        foreach (var item in components.Take(8))
        {
            lines.Add(PdfLine.Bullet(
                ValueOrDash(item.Label),
                $"Score {item.Score}, weight {item.Weight:0.##}, contribution {item.Contribution:0.##}"));
        }
    }

    private static void AddCurrentDashaSection(List<PdfLine> lines, CurrentDashaDto? dasha)
    {
        if (dasha is null)
        {
            return;
        }

        lines.Add(PdfLine.Space(7));
        lines.Add(PdfLine.Section("Current Dasha"));
        lines.Add(PdfLine.KeyValue("Mahadasha", $"{ValueOrDash(dasha.Mahadasha)} ({FormatDate(dasha.StartDate)} - {FormatDate(dasha.EndDate)})"));
        lines.Add(PdfLine.KeyValue("Antardasha", $"{ValueOrDash(dasha.Antardasha)} ({FormatDate(dasha.AntardashaStartDate)} - {FormatDate(dasha.AntardashaEndDate)})"));
        AddParagraph(lines, dasha.Summary, 900);
    }

    private static void AddMahadashaTimelineSection(List<PdfLine> lines, List<MahadashaPeriodDto>? periods)
    {
        if (periods is null || periods.Count == 0)
        {
            return;
        }

        lines.Add(PdfLine.Space(7));
        lines.Add(PdfLine.Section("Mahadasha Timeline"));
        lines.Add(PdfLine.TableHeader("Planet", "Start", "End", "Antardashas"));
        foreach (var period in periods.Take(12))
        {
            var antardashas = period.Antardashas?.Count > 0
                ? string.Join(", ", period.Antardashas.Take(4).Select(a => $"{a.SubPlanet} {FormatDate(a.StartDate)}"))
                : "-";
            lines.Add(PdfLine.TableRow(period.Planet, FormatDate(period.Start), FormatDate(period.End), antardashas));
        }
    }

    private static void AddSudarshanSection(List<PdfLine> lines, SudarshanChakraDto? sudarshan)
    {
        if (sudarshan is null)
        {
            return;
        }

        lines.Add(PdfLine.Space(7));
        lines.Add(PdfLine.Section("Sudarshan Chakra Kundali"));
        lines.Add(PdfLine.KeyValue("Score", sudarshan.OverallScore.ToString(CultureInfo.InvariantCulture)));
        AddParagraph(lines, FirstNonEmpty(sudarshan.OverallPrediction, sudarshan.Purpose, sudarshan.Usage), 900);

        if (sudarshan.HouseEmphasis.Count > 0)
        {
            lines.Add(PdfLine.TableHeader("House", "Theme", "Planets", "Prediction"));
            foreach (var house in sudarshan.HouseEmphasis.OrderByDescending(h => h.TotalPlanetCount).Take(6))
            {
                lines.Add(PdfLine.TableRow(
                    house.HouseNumber.ToString(CultureInfo.InvariantCulture),
                    ValueOrDash(house.Theme),
                    house.TotalPlanetCount.ToString(CultureInfo.InvariantCulture),
                    ValueOrDash(house.Prediction)));
            }
        }

        foreach (var item in sudarshan.Interpretations.Take(6))
        {
            lines.Add(PdfLine.Bullet($"{item.Area} ({item.Score})", FirstNonEmpty(item.Prediction, item.Tone, JoinCompact(item.Factors, null, 100))));
        }
    }

    private static void AddRemediesSection(List<PdfLine> lines, RemediesRecommendationDto? remedies)
    {
        if (remedies is null)
        {
            return;
        }

        lines.Add(PdfLine.Space(7));
        lines.Add(PdfLine.Section("Remedies Recommendation"));
        AddParagraph(lines, remedies.Summary, 700);
        AddRemedySelection(lines, "Primary", remedies.Primary);
        AddRemedySelection(lines, "Alternative", remedies.Alternative);

        if (remedies.Categories.Count > 0)
        {
            lines.Add(PdfLine.TableHeader("Category", "Planet", "Remedy", "Score", "Suitability"));
            foreach (var category in remedies.Categories.Take(8))
            {
                lines.Add(PdfLine.TableRow(category.Category, category.Planet, category.RecommendedRemedy, category.Score.ToString(CultureInfo.InvariantCulture), category.Suitability));
            }
        }

        foreach (var avoid in remedies.Avoid.Take(5))
        {
            lines.Add(PdfLine.Bullet($"Avoid {avoid.RemedyName}", $"{avoid.Planet}: {avoid.Reason}"));
        }

        AddParagraph(lines, remedies.Disclaimer, 420);
    }

    private static void AddRemedySelection(List<PdfLine> lines, string label, RemedySelectionDto? selection)
    {
        if (selection is null || string.IsNullOrWhiteSpace(selection.Name + selection.EnglishName + selection.Planet))
        {
            return;
        }

        var remedyName = FirstNonEmpty(selection.Name, selection.EnglishName, selection.SanskritName, selection.RemedyType);
        lines.Add(PdfLine.KeyValue($"{label} Remedy", $"{remedyName} for {ValueOrDash(selection.Planet)} - confidence {selection.Confidence}"));
        AddParagraph(lines, selection.Reason, 500);

        var gemstone = selection.Remedies?.Gemstone?.Primary;
        if (gemstone is not null && !string.IsNullOrWhiteSpace(gemstone.Name + gemstone.EnglishName))
        {
            lines.Add(PdfLine.Bullet("Gemstone", $"{FirstNonEmpty(gemstone.Name, gemstone.EnglishName)} - {ValueOrDash(gemstone.Purpose)}"));
        }

        var wearing = selection.Remedies?.Gemstone?.WearingRules;
        if (wearing is not null && !string.IsNullOrWhiteSpace(wearing.Finger + wearing.DayToWear + wearing.Time))
        {
            lines.Add(PdfLine.Bullet("Wearing", $"{ValueOrDash(wearing.Finger)}, {ValueOrDash(wearing.DayToWear)}, {ValueOrDash(wearing.Time)}, metal: {JoinCompact(wearing.Metal, null, 60)}"));
        }
    }

    private static void AddTransitSection(List<PdfLine> lines, TransitInsightDto? transit)
    {
        if (transit is null)
        {
            return;
        }

        lines.Add(PdfLine.Space(7));
        lines.Add(PdfLine.Section("Transit Insight"));
        lines.Add(PdfLine.KeyValue("Place", ValueOrDash(transit.Place)));
        lines.Add(PdfLine.KeyValue("Natal Ascendant", ValueOrDash(transit.NatalAscendant)));

        if (transit.Activations.Count > 0)
        {
            lines.Add(PdfLine.TableHeader("Planet", "Sign", "House", "Focus"));
            foreach (var activation in transit.Activations.Take(10))
            {
                lines.Add(PdfLine.TableRow(activation.Planet, activation.Sign, activation.NatalHouse.ToString(CultureInfo.InvariantCulture), ValueOrDash(activation.FocusArea)));
            }

            foreach (var activation in transit.Activations.Where(item => !string.IsNullOrWhiteSpace(item.InterpretationHint)).Take(4))
            {
                lines.Add(PdfLine.Bullet($"{activation.Planet} transit", CleanText(activation.InterpretationHint, 120)));
            }
        }
    }
    private static void AddParagraph(List<PdfLine> lines, string? text, int maxChars)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return;
        }

        var cleaned = CleanText(text, maxChars);
        foreach (var segment in cleaned.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries))
        {
            lines.Add(PdfLine.Paragraph(segment.Trim()));
        }
    }

    private static void AddOptionalJsonSection(List<PdfLine> lines, string title, object? value, int maxChars)
    {
        if (value is null)
        {
            return;
        }

        var text = JsonSerializer.Serialize(value, JsonOptions)
            .Replace("{", string.Empty)
            .Replace("}", string.Empty)
            .Replace("\"", string.Empty)
            .Replace(",", "; ");

        if (string.IsNullOrWhiteSpace(text))
        {
            return;
        }

        lines.Add(PdfLine.Space(7));
        lines.Add(PdfLine.Section(title));
        AddParagraph(lines, text, maxChars);
    }

    private static string CleanText(string text, int maxChars)
    {
        var cleaned = text.Trim();
        if (cleaned.Length > maxChars)
        {
            cleaned = cleaned[..maxChars].TrimEnd() + "...";
        }

        return cleaned;
    }

    private static string ScoreBand(int score, bool challenge = false)
    {
        if (challenge)
        {
            return score >= 70 ? "High attention" : score >= 45 ? "Moderate attention" : "Low pressure";
        }

        return score >= 80 ? "Very strong" : score >= 65 ? "Strong" : score >= 45 ? "Moderate" : "Needs support";
    }

    private static string FormatDate(DateTime value)
    {
        return value == default ? "-" : value.ToString("dd MMM yyyy", CultureInfo.InvariantCulture);
    }

    private static string JoinCompact(IEnumerable<string>? primary, IEnumerable<string>? fallback, int maxChars)
    {
        var values = (primary?.Where(v => !string.IsNullOrWhiteSpace(v)).ToList() ?? new List<string>());
        if (values.Count == 0 && fallback is not null)
        {
            values = fallback.Where(v => !string.IsNullOrWhiteSpace(v)).ToList();
        }

        if (values.Count == 0)
        {
            return "-";
        }

        return CleanText(string.Join("; ", values.Take(5)), maxChars);
    }

    private static string FirstNonEmpty(params string?[] values)
    {
        foreach (var value in values)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                return value.Trim();
            }
        }

        return "-";
    }
    private static string? BuildPersonDisplayName(JsonElement birthProfile, string? fallback)
    {
        var givenName = GetString(birthProfile, "fullName", "name");
        if (string.IsNullOrWhiteSpace(givenName))
        {
            givenName = fallback;
        }

        givenName = string.IsNullOrWhiteSpace(givenName) ? null : givenName.Trim();
        var surname = GetString(birthProfile, "surname", "Surname");
        surname = string.IsNullOrWhiteSpace(surname) ? null : surname.Trim();

        if (givenName is null)
        {
            return surname;
        }

        if (surname is null || givenName.EndsWith(surname, StringComparison.OrdinalIgnoreCase))
        {
            return givenName;
        }

        return $"{givenName} {surname}";
    }

    private static string[] BuildFamilyProfileCells(JsonElement birthProfile)
    {
        var cells = new List<string>();
        void add(string label, params string[] names)
        {
            var value = GetString(birthProfile, names);
            if (!string.IsNullOrWhiteSpace(value))
            {
                cells.Add(label);
                cells.Add(value.Trim());
            }
        }

        add("Surname", "surname", "Surname");
        add("Father", "fatherName", "FatherName");
        add("Mother", "motherName", "MotherName");
        add("Gotra", "gotra", "Gotra");
        return cells.ToArray();
    }

    private static JsonElement GetObject(JsonElement element, params string[] names)
    {
        if (element.ValueKind != JsonValueKind.Object)
        {
            return default;
        }

        foreach (var name in names)
        {
            if (element.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.Object)
            {
                return value;
            }
        }

        return default;
    }

    private static string? GetString(JsonElement element, params string[] names)
    {
        if (element.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        foreach (var name in names)
        {
            if (element.TryGetProperty(name, out var value))
            {
                return value.ValueKind == JsonValueKind.String ? value.GetString() : value.ToString();
            }
        }

        return null;
    }

    private static string? GetProperty(object value, params string[] names)
    {
        var type = value.GetType();
        foreach (var name in names)
        {
            var property = type.GetProperty(name);
            var propertyValue = property?.GetValue(value);
            if (propertyValue is not null && !string.IsNullOrWhiteSpace(propertyValue.ToString()))
            {
                return propertyValue.ToString();
            }
        }

        return null;
    }

    private static string ValueOrDash(string? value) => string.IsNullOrWhiteSpace(value) ? "-" : value.Trim();
    private static string PlanetStrength(PlanetDto planet)
    {
        var value = (planet.ConditionTone + " " + planet.DetailedStrength).ToLowerInvariant();
        if (value.Contains("weak") || value.Contains("debilitat") || value.Contains("afflict")) return "Weak";
        if (value.Contains("strong") || value.Contains("exalt") || value.Contains("own") || value.Contains("support")) return "Strong";
        return "Mixed";
    }
    private static string PlanetIcon(string? planet) => (planet ?? string.Empty).Trim().ToLowerInvariant() switch
    {
        "sun" => "☉", "moon" => "☽", "mars" => "♂", "mercury" => "☿", "jupiter" => "♃", "venus" => "♀", "saturn" => "♄", "rahu" => "☊", "ketu" => "☋", _ => "✦"
    };

    private static string SanitizeFileName(string fileName)
    {
        var sanitized = string.Join("_", fileName.Trim().Split(Path.GetInvalidFileNameChars()));
        if (!sanitized.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
        {
            sanitized += ".pdf";
        }

        return string.IsNullOrWhiteSpace(sanitized) ? "kundali-report.pdf" : sanitized;
    }

    private sealed record PdfDocument(string Brand, string Title, string Subtitle, IReadOnlyList<PdfLine> Lines);

    private sealed record PdfLine(PdfLineKind Kind, string Text, string[] Cells, double Height = 0)
    {
        public static PdfLine CoverTitle(string text) => new(PdfLineKind.CoverTitle, text, Array.Empty<string>());
        public static PdfLine CoverSubTitle(string text) => new(PdfLineKind.CoverSubTitle, text, Array.Empty<string>());
        public static PdfLine Meta(string text) => new(PdfLineKind.Meta, text, Array.Empty<string>());
        public static PdfLine Section(string text) => new(PdfLineKind.Section, text, Array.Empty<string>());
        public static PdfLine KeyValue(string key, string value) => new(PdfLineKind.KeyValue, key, new[] { key, value });
        public static PdfLine Grid(params string[] cells) => new(PdfLineKind.Grid, string.Empty, cells);
        public static PdfLine Paragraph(string text) => new(PdfLineKind.Paragraph, text, Array.Empty<string>());
        public static PdfLine Bullet(string title, string text) => new(PdfLineKind.Bullet, title, new[] { title, text });
        public static PdfLine TableHeader(params string[] cells) => new(PdfLineKind.TableHeader, string.Empty, cells);
        public static PdfLine ChartVisual(string title, string note, string[] houses) => new(PdfLineKind.ChartVisual, title, new[] { title, note }.Concat(houses.Take(12)).ToArray(), 184);
        public static PdfLine TableRow(params string[] cells) => new(PdfLineKind.TableRow, string.Empty, cells);
        public static PdfLine Space(double height = 10) => new(PdfLineKind.Space, string.Empty, Array.Empty<string>(), height);
    }

    private enum PdfLineKind
    {
        CoverTitle,
        CoverSubTitle,
        Meta,
        Section,
        KeyValue,
        Grid,
        Paragraph,
        Bullet,
        TableHeader,
        TableRow,
        ChartVisual,
        Space
    }

    private sealed record LayoutLine(PdfLineKind Kind, string Text, string[] Cells, double Height, int RowIndex);

    private static class StyledPdfWriter
    {
        private const double PageWidth = 595;
        private const double PageHeight = 842;
        private const double MarginLeft = 42;
        private const double MarginRight = 42;
        private const double HeaderTop = 36;
        private const double FirstPageContentTop = 142;
        private const double RunningPageContentTop = 62;
        private const double FooterBottom = 42;
        private const double ContentWidth = PageWidth - MarginLeft - MarginRight;

        public static byte[] Write(PdfDocument document)
        {
            var pages = Paginate(document.Lines);
            var objects = new List<string>
            {
                "<< /Type /Catalog /Pages 2 0 R >>",
                string.Empty,
                "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>",
                "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica-Bold >>",
                "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica-Oblique >>"
            };

            var pageObjectNumbers = new List<int>();
            for (var pageIndex = 0; pageIndex < pages.Count; pageIndex++)
            {
                var contentObjectNumber = objects.Count + 2;
                var pageObjectNumber = objects.Count + 1;
                pageObjectNumbers.Add(pageObjectNumber);
                objects.Add($"<< /Type /Page /Parent 2 0 R /MediaBox [0 0 {Fmt(PageWidth)} {Fmt(PageHeight)}] /Resources << /Font << /F1 3 0 R /F2 4 0 R /F3 5 0 R >> >> /Contents {contentObjectNumber} 0 R >>");
                var stream = BuildContentStream(document, pages[pageIndex], pageIndex + 1, pages.Count);
                objects.Add($"<< /Length {Encoding.ASCII.GetByteCount(stream)} >>\nstream\n{stream}endstream");
            }

            objects[1] = $"<< /Type /Pages /Kids [{string.Join(" ", pageObjectNumbers.Select(n => $"{n} 0 R"))}] /Count {pageObjectNumbers.Count} >>";
            return BuildPdf(objects);
        }

        private static List<List<LayoutLine>> Paginate(IReadOnlyList<PdfLine> sourceLines)
        {
            var pages = new List<List<LayoutLine>>();
            var current = new List<LayoutLine>();
            var pageIndex = 0;
            var y = PageHeight - GetContentTop(pageIndex);
            var rowIndex = 0;

            foreach (var line in sourceLines.SelectMany(WrapLine))
            {
                var height = MeasureLineHeight(line);
                if (y - height < FooterBottom && current.Count > 0)
                {
                    pages.Add(current);
                    current = new List<LayoutLine>();
                    pageIndex++;
                    y = PageHeight - GetContentTop(pageIndex);
                    rowIndex = 0;
                }

                current.Add(new LayoutLine(line.Kind, line.Text, line.Cells, height, rowIndex++));
                y -= height;
            }

            if (current.Count > 0)
            {
                pages.Add(current);
            }

            return pages.Count > 0 ? pages : new List<List<LayoutLine>> { new() };
        }

        private static IEnumerable<PdfLine> WrapLine(PdfLine line)
        {
            if (line.Kind is PdfLineKind.Space or PdfLineKind.TableHeader or PdfLineKind.TableRow or PdfLineKind.KeyValue or PdfLineKind.Grid or PdfLineKind.ChartVisual
                || line.Text.Length <= MaxChars(line.Kind))
            {
                yield return line;
                yield break;
            }

            foreach (var text in WrapText(line.Text, MaxChars(line.Kind)))
            {
                yield return new PdfLine(line.Kind, text, line.Cells, line.Height);
            }
        }

        private static IEnumerable<string> WrapText(string text, int maxChars)
        {
            var words = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var builder = new StringBuilder();
            foreach (var word in words)
            {
                if (builder.Length + word.Length + 1 > maxChars && builder.Length > 0)
                {
                    yield return builder.ToString();
                    builder.Clear();
                }

                if (builder.Length > 0)
                {
                    builder.Append(' ');
                }

                builder.Append(word);
            }

            if (builder.Length > 0)
            {
                yield return builder.ToString();
            }
        }

        private static string BuildContentStream(PdfDocument document, IReadOnlyList<LayoutLine> page, int pageNumber, int pageCount)
        {
            var builder = new StringBuilder();
            DrawHeader(builder, document, pageNumber == 1);

            var y = PageHeight - GetContentTop(pageNumber - 1);
            foreach (var line in page)
            {
                DrawLine(builder, line, y);
                y -= line.Height;
            }

            DrawFooter(builder, pageNumber, pageCount);
            return builder.ToString();
        }

        private static double GetContentTop(int pageIndex)
        {
            return pageIndex == 0 ? FirstPageContentTop : RunningPageContentTop;
        }

        private static void DrawHeader(StringBuilder builder, PdfDocument document, bool firstPage)
        {
            if (!firstPage)
            {
                DrawRect(builder, 0, PageHeight - 34, PageWidth, 34, "FFFFFF");
                DrawRect(builder, 0, PageHeight - 37, PageWidth, 2, "D8643A");
                DrawText(builder, "HoraLife", MarginLeft, PageHeight - 22, 11, "F2", "D8643A");
                DrawText(builder, document.Title, MarginLeft + 68, PageHeight - 22, 8.5, "F1", "5F6673");
                return;
            }

            DrawRect(builder, 0, PageHeight - 96, PageWidth, 96, "FFFFFF");
            DrawRect(builder, 0, PageHeight - 100, PageWidth, 3, "D8643A");
            DrawHoraMark(builder, 54, PageHeight - 49, "D8643A");
            DrawText(builder, "Hora", 86, PageHeight - 44, 24, "F2", "D8643A");
            DrawText(builder, "Life  |  Explainable Vedic Astrology", 150, PageHeight - 44, 10.5, "F2", "5F6673");
            DrawText(builder, document.Title, 88, PageHeight - 66, 10.5, "F1", "5F6673");
            DrawText(builder, document.Subtitle, MarginLeft, PageHeight - 118, 9.5, "F3", "5F6673");
        }

        private static void DrawFooter(StringBuilder builder, int pageNumber, int pageCount)
        {
            DrawRect(builder, MarginLeft, 38, ContentWidth, 0.8, "E5E7EB");
            DrawText(builder, "Confidential - generated by HoraLife", MarginLeft, 24, 8.5, "F1", "5F6673");
            DrawText(builder, $"Page {pageNumber} of {pageCount}", PageWidth - MarginRight - 58, 24, 8.5, "F1", "5F6673");
        }

        private static void DrawLine(StringBuilder builder, LayoutLine line, double y)
        {
            switch (line.Kind)
            {
                case PdfLineKind.CoverTitle:
                    DrawText(builder, line.Text, MarginLeft, y, 22, "F2", "111827");
                    break;
                case PdfLineKind.CoverSubTitle:
                    DrawText(builder, line.Text, MarginLeft, y, 13, "F1", "5F6673");
                    break;
                case PdfLineKind.Meta:
                    DrawPill(builder, MarginLeft, y - 5, 190, 18, line.Text);
                    break;
                case PdfLineKind.Section:
                    DrawRect(builder, MarginLeft, y - 4, ContentWidth, 20, "F8FAFC");
                    DrawRect(builder, MarginLeft, y - 4, 4, 20, "D8643A");
                    DrawText(builder, line.Text.ToUpperInvariant(), MarginLeft + 12, y + 2, 10.5, "F2", "111827");
                    break;
                case PdfLineKind.KeyValue:
                    DrawKeyValue(builder, line.Cells, y);
                    break;
                case PdfLineKind.Grid:
                    DrawGrid(builder, line.Cells, y);
                    break;
                case PdfLineKind.Paragraph:
                    DrawText(builder, line.Text, MarginLeft + 4, y, 9.8, "F1", "111827");
                    break;
                case PdfLineKind.Bullet:
                    DrawBullet(builder, line.Cells, y);
                    break;
                case PdfLineKind.TableHeader:
                    DrawTableRow(builder, line.Cells, y, line.Height, true, false);
                    break;
                case PdfLineKind.TableRow:
                    DrawTableRow(builder, line.Cells, y, line.Height, false, line.RowIndex % 2 == 0);
                    break;
                case PdfLineKind.ChartVisual:
                    DrawChartVisual(builder, line.Cells, y);
                    break;
                case PdfLineKind.Space:
                    break;
            }
        }

        private static void DrawKeyValue(StringBuilder builder, string[] cells, double y)
        {
            var key = cells.ElementAtOrDefault(0) ?? string.Empty;
            var value = cells.ElementAtOrDefault(1) ?? string.Empty;
            DrawRect(builder, MarginLeft, y - 5, ContentWidth, 18, "FFFFFF");
            DrawText(builder, key, MarginLeft + 9, y, 9.3, "F2", "D8643A");
            DrawText(builder, Truncate(value, 74), MarginLeft + 132, y, 9.3, "F1", "111827");
        }

        private static void DrawGrid(StringBuilder builder, string[] cells, double y)
        {
            var pairCount = Math.Max(1, cells.Length / 2);
            var columnWidth = ContentWidth / pairCount;
            DrawRect(builder, MarginLeft, y - 5, ContentWidth, 18, "FFFFFF");

            for (var pair = 0; pair < pairCount; pair++)
            {
                var key = cells.ElementAtOrDefault(pair * 2) ?? string.Empty;
                var value = cells.ElementAtOrDefault(pair * 2 + 1) ?? string.Empty;
                var x = MarginLeft + pair * columnWidth + 8;
                DrawText(builder, key, x, y, 8.4, "F2", "D8643A");
                DrawText(builder, Truncate(value, MaxCellChars(columnWidth - 52)), x + 54, y, 8.4, "F1", "111827");
            }
        }
        private static void DrawBullet(StringBuilder builder, string[] cells, double y)
        {
            var title = Truncate(cells.ElementAtOrDefault(0) ?? "-", 28);
            var text = Truncate(cells.ElementAtOrDefault(1) ?? "-", 76);
            DrawText(builder, "-", MarginLeft + 6, y, 10, "F2", "D8643A");
            DrawText(builder, title, MarginLeft + 18, y, 9.5, "F2", "111827");
            DrawText(builder, text, MarginLeft + 172, y, 9.2, "F1", "5F6673");
        }

        private static void DrawChartVisual(StringBuilder builder, string[] cells, double y)
        {
            var title = cells.ElementAtOrDefault(0) ?? "Kundali";
            var note = cells.ElementAtOrDefault(1) ?? string.Empty;
            var houses = cells.Skip(2).Take(12).Concat(Enumerable.Repeat("-", 12)).Take(12).ToArray();
            const double size = 142;
            var x = MarginLeft + 8;
            var top = y - 18;
            var bottom = top - size;

            DrawRect(builder, MarginLeft, y - 178, ContentWidth, 174, "FFFFFF");
            DrawText(builder, title, MarginLeft + 8, y, 10.5, "F2", "111827");
            DrawText(builder, Truncate(note, 76), MarginLeft + 190, y, 8.4, "F1", "5F6673");

            DrawStrokeRect(builder, x, bottom, size, size, "D8643A", 1.0);
            DrawStrokeLine(builder, x, top, x + size, bottom, "D8643A", 0.7);
            DrawStrokeLine(builder, x + size, top, x, bottom, "D8643A", 0.7);
            DrawStrokeLine(builder, x + size / 2, top, x + size, bottom + size / 2, "D8643A", 0.7);
            DrawStrokeLine(builder, x + size, bottom + size / 2, x + size / 2, bottom, "D8643A", 0.7);
            DrawStrokeLine(builder, x + size / 2, bottom, x, bottom + size / 2, "D8643A", 0.7);
            DrawStrokeLine(builder, x, bottom + size / 2, x + size / 2, top, "D8643A", 0.7);

            var positions = new[]
            {
                (x + 63, top - 28), (x + 36, top - 47), (x + 10, top - 25), (x + 16, bottom + 70),
                (x + 10, bottom + 30), (x + 36, bottom + 48), (x + 63, bottom + 24), (x + 92, bottom + 48),
                (x + 106, bottom + 30), (x + 104, bottom + 70), (x + 106, top - 25), (x + 92, top - 47)
            };

            for (var i = 0; i < 12; i++)
            {
                var (hx, hy) = positions[i];
                DrawText(builder, (i + 1).ToString(CultureInfo.InvariantCulture), hx, hy + 8, 5.8, "F2", "D8643A");
                var content = Truncate(houses[i], 14);
                if (content != "-")
                {
                    DrawText(builder, content, hx, hy, 5.9, "F1", "111827");
                }
            }

            DrawChartLegend(builder, houses, MarginLeft + 176, y - 22);
        }

        private static void DrawChartLegend(StringBuilder builder, string[] houses, double x, double y)
        {
            DrawText(builder, "House placements", x, y, 8.6, "F2", "D8643A");
            for (var i = 0; i < 12; i++)
            {
                var col = i / 6;
                var row = i % 6;
                var lx = x + col * 156;
                var ly = y - 18 - row * 18;
                DrawText(builder, $"H{i + 1}", lx, ly, 7.7, "F2", "5F6673");
                DrawText(builder, Truncate(houses[i], 25), lx + 24, ly, 7.7, "F1", "111827");
            }
        }
        private static void DrawTableRow(StringBuilder builder, string[] cells, double y, double rowHeight, bool header, bool shaded)
        {
            var bg = header ? "D8643A" : shaded ? "F8FAFC" : "FFFFFF";
            var fg = header ? "FFFFFF" : "111827";
            DrawRect(builder, MarginLeft, y - 5, ContentWidth, rowHeight - 1, bg);
            DrawRect(builder, MarginLeft, y - 5, ContentWidth, 0.4, "E5E7EB");

            var columns = GetColumnWidths(cells.Length);
            var x = MarginLeft + 7;
            for (var i = 0; i < cells.Length && i < columns.Length; i++)
            {
                var wrapped = header
                    ? new[] { Truncate(cells[i], MaxCellChars(columns[i])) }
                    : WrapText(cells[i] ?? string.Empty, MaxCellChars(columns[i])).Take(3).ToArray();
                for (var lineIndex = 0; lineIndex < wrapped.Length; lineIndex++)
                {
                    DrawText(builder, wrapped[lineIndex], x, y - (lineIndex * 9.8), header ? 8.5 : 8.0, header ? "F2" : "F1", fg);
                }
                x += columns[i];
            }
        }

        private static double[] GetColumnWidths(int count) => count switch
        {
            6 => new[] { 72d, 72d, 62d, 52d, 122d, 116d },
            5 => new[] { 62d, 82d, 92d, 194d, 58d },
            4 => new[] { 78d, 72d, 54d, 300d },
            3 => new[] { 110d, 110d, 282d },
            _ => Enumerable.Repeat(ContentWidth / Math.Max(count, 1), Math.Max(count, 1)).ToArray()
        };

        private static int MaxCellChars(double width) => Math.Max(6, (int)Math.Floor(width / 5.1));

        private static double MeasureLineHeight(PdfLine line)
        {
            if (line.Kind == PdfLineKind.TableRow)
            {
                var columns = GetColumnWidths(line.Cells.Length);
                var maxLines = 1;
                for (var i = 0; i < line.Cells.Length && i < columns.Length; i++)
                {
                    maxLines = Math.Max(maxLines, Math.Min(3, WrapText(line.Cells[i] ?? string.Empty, MaxCellChars(columns[i])).Count()));
                }

                return 10 + maxLines * 9.8;
            }

            if (line.Kind == PdfLineKind.TableHeader)
            {
                return 19;
            }

            return GetLineHeight(line.Kind, line.Height);
        }
        private static double GetLineHeight(PdfLineKind kind, double customHeight) => kind switch
        {
            PdfLineKind.CoverTitle => 31,
            PdfLineKind.CoverSubTitle => 22,
            PdfLineKind.Meta => 26,
            PdfLineKind.Section => 27,
            PdfLineKind.KeyValue => 20,
            PdfLineKind.TableHeader => 19,
            PdfLineKind.TableRow => 19,
            PdfLineKind.ChartVisual => customHeight > 0 ? customHeight : 184,
            PdfLineKind.Bullet => 17,
            PdfLineKind.Space => customHeight,
            _ => 14
        };

        private static int MaxChars(PdfLineKind kind) => kind switch
        {
            PdfLineKind.CoverTitle => 40,
            PdfLineKind.CoverSubTitle => 62,
            PdfLineKind.Meta => 78,
            PdfLineKind.Section => 50,
            PdfLineKind.Bullet => 92,
            _ => 104
        };

        private static void DrawPill(StringBuilder builder, double x, double y, double width, double height, string text)
        {
            DrawRect(builder, x, y, width, height, "F8FAFC");
            DrawText(builder, text, x + 9, y + 6, 8.5, "F2", "D8643A");
        }

        private static void DrawRect(StringBuilder builder, double x, double y, double width, double height, string hex)
        {
            var (r, g, b) = Rgb(hex);
            builder.AppendLine("q");
            builder.AppendLine($"{Fmt(r)} {Fmt(g)} {Fmt(b)} rg");
            builder.AppendLine($"{Fmt(x)} {Fmt(y)} {Fmt(width)} {Fmt(height)} re f");
            builder.AppendLine("Q");
        }

        private static void DrawStrokeRect(StringBuilder builder, double x, double y, double width, double height, string hex, double strokeWidth)
        {
            var (r, g, b) = Rgb(hex);
            builder.AppendLine("q");
            builder.AppendLine($"{Fmt(r)} {Fmt(g)} {Fmt(b)} RG");
            builder.AppendLine($"{Fmt(strokeWidth)} w");
            builder.AppendLine($"{Fmt(x)} {Fmt(y)} {Fmt(width)} {Fmt(height)} re S");
            builder.AppendLine("Q");
        }

        private static void DrawStrokeLine(StringBuilder builder, double x1, double y1, double x2, double y2, string hex, double strokeWidth)
        {
            var (r, g, b) = Rgb(hex);
            builder.AppendLine("q");
            builder.AppendLine($"{Fmt(r)} {Fmt(g)} {Fmt(b)} RG");
            builder.AppendLine($"{Fmt(strokeWidth)} w");
            builder.AppendLine($"{Fmt(x1)} {Fmt(y1)} m {Fmt(x2)} {Fmt(y2)} l S");
            builder.AppendLine("Q");
        }
        private static void DrawHoraMark(StringBuilder builder, double cx, double cy, string hex)
        {
            var (r, g, b) = Rgb(hex);
            builder.AppendLine("q");
            builder.AppendLine($"{Fmt(r)} {Fmt(g)} {Fmt(b)} RG");
            builder.AppendLine("1.2 w");
            builder.AppendLine($"{Fmt(cx)} {Fmt(cy + 18)} m {Fmt(cx + 18)} {Fmt(cy)} l {Fmt(cx)} {Fmt(cy - 18)} l {Fmt(cx - 18)} {Fmt(cy)} l h S");
            builder.AppendLine("0.6 w");
            builder.AppendLine($"{Fmt(cx)} {Fmt(cy + 18)} m {Fmt(cx + 18)} {Fmt(cy)} l {Fmt(cx)} {Fmt(cy - 18)} l {Fmt(cx - 18)} {Fmt(cy)} l {Fmt(cx)} {Fmt(cy + 18)} l S");
            builder.AppendLine("Q");
            DrawRect(builder, cx - 2.2, cy - 2.2, 4.4, 4.4, hex);
        }

        private static void DrawText(StringBuilder builder, string text, double x, double y, double size, string font, string hex)
        {
            var (r, g, b) = Rgb(hex);
            builder.AppendLine("BT");
            builder.AppendLine($"{Fmt(r)} {Fmt(g)} {Fmt(b)} rg");
            builder.AppendLine($"/{font} {Fmt(size)} Tf");
            builder.AppendLine($"1 0 0 1 {Fmt(x)} {Fmt(y)} Tm");
            builder.AppendLine($"({EscapePdfText(text)}) Tj");
            builder.AppendLine("ET");
        }

        private static byte[] BuildPdf(IReadOnlyList<string> objects)
        {
            using var ms = new MemoryStream();
            using var writer = new StreamWriter(ms, Encoding.ASCII, leaveOpen: true) { NewLine = "\n" };
            writer.WriteLine("%PDF-1.4");
            var offsets = new List<long> { 0 };
            for (var i = 0; i < objects.Count; i++)
            {
                writer.Flush();
                offsets.Add(ms.Position);
                writer.WriteLine($"{i + 1} 0 obj");
                writer.WriteLine(objects[i]);
                writer.WriteLine("endobj");
            }

            writer.Flush();
            var xrefOffset = ms.Position;
            writer.WriteLine("xref");
            writer.WriteLine($"0 {objects.Count + 1}");
            writer.WriteLine("0000000000 65535 f ");
            foreach (var offset in offsets.Skip(1))
            {
                writer.WriteLine($"{offset:0000000000} 00000 n ");
            }

            writer.WriteLine("trailer");
            writer.WriteLine($"<< /Size {objects.Count + 1} /Root 1 0 R >>");
            writer.WriteLine("startxref");
            writer.WriteLine(xrefOffset.ToString(CultureInfo.InvariantCulture));
            writer.WriteLine("%%EOF");
            writer.Flush();
            return ms.ToArray();
        }

        private static string Truncate(string value, int maxChars)
        {
            var clean = value.Replace("\r", " ").Replace("\n", " ").Trim();
            return clean.Length <= maxChars ? clean : clean[..Math.Max(0, maxChars - 3)].TrimEnd() + "...";
        }

        private static (double R, double G, double B) Rgb(string hex)
        {
            var value = hex.TrimStart('#');
            return (
                Convert.ToInt32(value[..2], 16) / 255d,
                Convert.ToInt32(value.Substring(2, 2), 16) / 255d,
                Convert.ToInt32(value.Substring(4, 2), 16) / 255d);
        }

        private static string Fmt(double value) => value.ToString("0.###", CultureInfo.InvariantCulture);

        private static string EscapePdfText(string text)
        {
            var ascii = new StringBuilder(text.Length);
            foreach (var ch in text)
            {
                ascii.Append(ch is >= ' ' and <= '~' ? ch : '?');
            }

            return ascii.ToString().Replace("\\", "\\\\").Replace("(", "\\(").Replace(")", "\\)");
        }
    }
}
