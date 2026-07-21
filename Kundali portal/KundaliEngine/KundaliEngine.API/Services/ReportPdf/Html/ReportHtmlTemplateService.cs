using System.Globalization;
using System.Net;
using System.Text;
using System.Text.Json;
using KundaliEngine.API.DTOs.Responses;

namespace KundaliEngine.API.Services.ReportPdf.Html;

public interface IReportHtmlTemplateService
{
    string BuildDesktopColorHtml(JsonElement birthProfile, GenerateKundaliResponse report);
    string BuildCorporateHtml(JsonElement birthProfile, GenerateKundaliResponse report);
    string BuildPrintBlackWhiteHtml(JsonElement birthProfile, GenerateKundaliResponse report);
}

public sealed class ReportHtmlTemplateService : IReportHtmlTemplateService
{
    private readonly IWebHostEnvironment _environment;

    public ReportHtmlTemplateService(IWebHostEnvironment environment)
    {
        _environment = environment;
    }

    public string BuildDesktopColorHtml(JsonElement birthProfile, GenerateKundaliResponse report)
    {
        var template = ReadTemplate("kundali-report.html");
        var styles = ReadCss("base.css") + Environment.NewLine + ReadCss("desktop-color.css");
        var body = BuildBody(birthProfile, report);
        return template
            .Replace("{{Title}}", Html("HoraLife Kundali Report"), StringComparison.Ordinal)
            .Replace("{{Styles}}", styles, StringComparison.Ordinal)
            .Replace("{{Body}}", body, StringComparison.Ordinal);
    }

    public string BuildCorporateHtml(JsonElement birthProfile, GenerateKundaliResponse report)
    {
        var template = ReadTemplate("kundali-report.html");
        var styles = ReadCss("base.css") + Environment.NewLine + ReadCss("corporate-advisory.css");
        var body = BuildCorporateBody(birthProfile, report);
        return template
            .Replace("{{Title}}", Html("Personal Vedic Astrology Advisory Report"), StringComparison.Ordinal)
            .Replace("{{Styles}}", styles, StringComparison.Ordinal)
            .Replace("{{Body}}", body, StringComparison.Ordinal);
    }

    public string BuildPrintBlackWhiteHtml(JsonElement birthProfile, GenerateKundaliResponse report)
    {
        var template = ReadTemplate("kundali-report.html");
        var styles = ReadCss("base.css") + Environment.NewLine + ReadCss("desktop-color.css") + Environment.NewLine + ReadCss("print-black-white.css");
        var body = BuildPrintBlackWhiteBody(birthProfile, report);
        return template
            .Replace("{{Title}}", Html("HoraLife Kundali Report"), StringComparison.Ordinal)
            .Replace("{{Styles}}", styles, StringComparison.Ordinal)
            .Replace("{{Body}}", body, StringComparison.Ordinal);
    }
    private string BuildBody(JsonElement birthProfile, GenerateKundaliResponse report)
    {
        var name = ValueOrDash(GetString(birthProfile, "fullName", "name") ?? report.Summary?.Name);
        var birthDate = ValueOrDash(GetString(birthProfile, "birthDate", "dateOfBirth"));
        var birthTime = ValueOrDash(GetString(birthProfile, "birthTime", "timeOfBirth"));
        var birthPlace = ValueOrDash(GetString(birthProfile, "birthPlace", "placeOfBirth", "birthplace"));
        var generatedOn = DateTime.Now.ToString("dd MMM yyyy, hh:mm tt", CultureInfo.InvariantCulture);
        var branding = GetObject(birthProfile, "branding", "whiteLabel", "astrologerBranding");
        var astrologerName = FirstNonEmpty(GetString(branding, "astrologerName", "name"), "Vasudev Sharna");
        var astrologerDesignation = FirstNonEmpty(GetString(branding, "designation", "role"), "Astrologer & Vastu consultant");
        var astrologerAddress = FirstNonEmpty(GetString(branding, "address"), "Ram mandir, Bhalki, Dist. Bidar 585328, Karnataka");
        var astrologerContact = FirstNonEmpty(GetString(branding, "contactNumber", "phone", "mobile"), "9035909039");

        var html = new StringBuilder();
        html.AppendLine("<main class=\"report-page\">");
        html.AppendLine("<header class=\"report-hero\">");
        html.AppendLine("<div>");
        html.AppendLine("<div class=\"brand-lockup\"><div class=\"hora-mark\">H</div><div class=\"brand-copy\"><h1>HoraLife Kundali Report</h1><span>Explainable Vedic Astrology</span></div></div>");
        html.AppendLine($"<p class=\"subject-line\">Prepared for <strong>{Html(name)}</strong> - {Html(birthDate)} - {Html(birthPlace)}</p>");
        html.AppendLine($"<p class=\"report-meta\">Generated {Html(generatedOn)}</p>");
        html.AppendLine("</div>");
        html.AppendLine($"<aside class=\"prepared-by\"><span>Prepared by</span><strong>{Html(astrologerName)} <em>{Html(astrologerDesignation)}</em></strong><span>{Html(astrologerAddress)}</span><span>{Html(astrologerContact)}</span></aside>");
        html.AppendLine("</header>");

        AppendSection(html, "Birth Profile", $"<div class=\"grid-3\">{Fact("Name", name)}{Fact("Date", birthDate)}{Fact("Time", birthTime)}</div><div style=\"height:7px\"></div><div class=\"grid-2\">{Fact("Place", birthPlace)}{Fact("Coordinates", BuildCoordinates(birthProfile))}</div>");
        AppendCoreIdentity(html, report);
        AppendMetadataInsights(html, report.RashiInsight, report.NakshatraInsight);
        AppendCharts(html, report);
        AppendScores(html, report.Scores);
        AppendBhavaScores(html, report.Houses);
        AppendPlanetAndHouseTables(html, report.Planets, report.Houses);
        AppendYogasDoshas(html, report);
        AppendDasha(html, report.CurrentDasha, report.MahadashaPeriods);
        AppendSudarshan(html, report);
        AppendTransit(html, report.TransitInsight);
        AppendRemedies(html, report.RemediesRecommendation);

        html.AppendLine("<footer class=\"report-footer\"><span>Confidential - generated by HoraLife</span><span>Desktop color theme</span></footer>");
        html.AppendLine("</main>");
        return html.ToString();
    }

    private string BuildPrintBlackWhiteBody(JsonElement birthProfile, GenerateKundaliResponse report)
    {
        var name = ValueOrDash(GetString(birthProfile, "fullName", "name") ?? report.Summary?.Name);
        var birthDate = ValueOrDash(GetString(birthProfile, "birthDate", "dateOfBirth"));
        var birthTime = ValueOrDash(GetString(birthProfile, "birthTime", "timeOfBirth"));
        var birthPlace = ValueOrDash(GetString(birthProfile, "birthPlace", "placeOfBirth", "birthplace"));
        var generatedOn = DateTime.Now.ToString("dd MMM yyyy, hh:mm tt", CultureInfo.InvariantCulture);
        var branding = GetObject(birthProfile, "branding", "whiteLabel", "astrologerBranding");
        var astrologerName = FirstNonEmpty(GetString(branding, "astrologerName", "name"), "Vasudev Sharna");
        var astrologerDesignation = FirstNonEmpty(GetString(branding, "designation", "role"), "Astrologer & Vastu consultant");
        var astrologerAddress = FirstNonEmpty(GetString(branding, "address"), "Ram mandir, Bhalki, Dist. Bidar 585328, Karnataka");
        var astrologerContact = FirstNonEmpty(GetString(branding, "contactNumber", "phone", "mobile"), "9035909039");

        var html = new StringBuilder();
        html.AppendLine("<main class=\"report-page print-bw-report\">");
        html.AppendLine($"<div class=\"print-running-header\"><span>HoraLife Kundali Report</span><strong>{Html(name)}</strong><span>{Html(generatedOn)}</span></div>");
        html.AppendLine("<div class=\"print-running-footer\"><span>Confidential - print copy</span><span>Generated by HoraLife</span><span class=\"page-counter\">Page </span></div>");
        html.AppendLine("<header class=\"report-hero print-bw-hero\">");
        html.AppendLine("<div>");
        html.AppendLine("<div class=\"brand-lockup\"><div class=\"hora-mark\">H</div><div class=\"brand-copy\"><h1>HoraLife Kundali Report</h1><span>Minimalist black and white print format</span></div></div>");
        html.AppendLine($"<p class=\"subject-line\">Prepared for <strong>{Html(name)}</strong> - {Html(birthDate)} - {Html(birthPlace)}</p>");
        html.AppendLine($"<p class=\"report-meta\">Generated {Html(generatedOn)}</p>");
        html.AppendLine("</div>");
        html.AppendLine($"<aside class=\"prepared-by\"><span>Prepared by</span><strong>{Html(astrologerName)} <em>{Html(astrologerDesignation)}</em></strong><span>{Html(astrologerAddress)}</span><span>{Html(astrologerContact)}</span></aside>");
        html.AppendLine("</header>");

        AppendSection(html, "Birth Profile", $"<div class=\"grid-3\">{Fact("Name", name)}{Fact("Date", birthDate)}{Fact("Time", birthTime)}</div><div style=\"height:7px\"></div><div class=\"grid-2\">{Fact("Place", birthPlace)}{Fact("Coordinates", BuildCoordinates(birthProfile))}</div>");
        AppendCoreIdentity(html, report);
        AppendMetadataInsights(html, report.RashiInsight, report.NakshatraInsight);
        AppendCharts(html, report);
        AppendScores(html, report.Scores);
        AppendBhavaScores(html, report.Houses);
        AppendPlanetAndHouseTables(html, report.Planets, report.Houses);
        AppendYogasDoshas(html, report);
        AppendDasha(html, report.CurrentDasha, report.MahadashaPeriods);
        AppendSudarshan(html, report);
        AppendTransit(html, report.TransitInsight);
        AppendRemedies(html, report.RemediesRecommendation);

        html.AppendLine("<footer class=\"report-footer print-document-footer\"><span>End of report</span><span>Black & white print format</span></footer>");
        html.AppendLine("</main>");
        return html.ToString();
    }

    private string BuildCorporateBody(JsonElement birthProfile, GenerateKundaliResponse report)
    {
        var name = ValueOrDash(GetString(birthProfile, "fullName", "name") ?? report.Summary?.Name);
        var birthDate = ValueOrDash(GetString(birthProfile, "birthDate", "dateOfBirth"));
        var birthTime = ValueOrDash(GetString(birthProfile, "birthTime", "timeOfBirth"));
        var birthPlace = ValueOrDash(GetString(birthProfile, "birthPlace", "placeOfBirth", "birthplace"));
        var generatedOn = DateTime.Now.ToString("dd MMM yyyy, hh:mm tt", CultureInfo.InvariantCulture);
        var branding = GetObject(birthProfile, "branding", "whiteLabel", "astrologerBranding");
        var astrologerName = FirstNonEmpty(GetString(branding, "astrologerName", "name"), "Vasudev Sharna");
        var astrologerDesignation = FirstNonEmpty(GetString(branding, "designation", "role"), "Astrologer & Vastu consultant");
        var astrologerAddress = FirstNonEmpty(GetString(branding, "address"), "Ram mandir, Bhalki, Dist. Bidar 585328, Karnataka");
        var astrologerContact = FirstNonEmpty(GetString(branding, "contactNumber", "phone", "mobile"), "9035909039");

        var html = new StringBuilder();
        html.AppendLine("<main class=\"report-page corporate-report\">");
        html.AppendLine($"<div class=\"running-header\"><span>Personal Vedic Astrology Advisory Report</span><strong>{Html(name)}</strong><span>{Html(generatedOn)}</span></div>");
        html.AppendLine("<div class=\"running-footer\"><span>Confidential advisory report</span><span>Generated by HoraLife</span><span class=\"page-counter\">Page </span></div>");
        html.AppendLine("<header class=\"report-hero corporate-hero\">");
        html.AppendLine("<div>");
        html.AppendLine("<div class=\"brand-lockup\"><div class=\"hora-mark\">H</div><div class=\"brand-copy\"><h1>Personal Vedic Astrology Advisory Report</h1><span>A structured analysis of birth profile, planetary indicators, life themes, timing cycles, and practical guidance.</span></div></div>");
        html.AppendLine($"<p class=\"subject-line\">Prepared for <strong>{Html(name)}</strong> - {Html(birthDate)} - {Html(birthPlace)}</p>");
        html.AppendLine($"<p class=\"report-meta\">Generated {Html(generatedOn)}</p>");
        html.AppendLine("</div>");
        html.AppendLine($"<aside class=\"prepared-by\"><span>Prepared by</span><strong>{Html(astrologerName)} <em>{Html(astrologerDesignation)}</em></strong><span>{Html(astrologerAddress)}</span><span>{Html(astrologerContact)}</span></aside>");
        html.AppendLine("</header>");

        AppendSection(html, "Executive Summary", BuildCorporateExecutiveSummary(report));
        AppendSection(html, "Birth Profile", $"<p class=\"report-text\">The birth profile establishes the reference point for all subsequent chart interpretation.</p><div class=\"grid-3\">{Fact("Name", name)}{Fact("Date", birthDate)}{Fact("Time", birthTime)}</div><div style=\"height:7px\"></div><div class=\"grid-2\">{Fact("Place", birthPlace)}{Fact("Coordinates", BuildCoordinates(birthProfile))}</div>");
        AppendCorporateCoreIndicators(html, report);
        AppendCorporatePlanetaryAnalysis(html, report.Planets);
        AppendCorporateBhavaReview(html, report.Houses, report.Scores);
        AppendCorporateYogasDoshas(html, report);
        AppendCorporateCurrentPeriod(html, report.CurrentDasha, report.MahadashaPeriods);
        AppendCorporateTransit(html, report.TransitInsight);
        AppendCorporateRemedies(html, report.RemediesRecommendation);
        AppendSection(html, "Conclusion", "<p class=\"report-text\">Jyotisha is treated here as a timing and reflection framework: the chart indicates tendencies, while choices shape outcomes. A strong period is most useful when paired with disciplined action; a sensitive period is best handled with planning and patience.</p>");

        html.AppendLine("<footer class=\"report-footer document-footer\"><span>End of report</span><span>Confidential advisory report</span></footer>");
        html.AppendLine("</main>");
        return html.ToString();
    }

    private static string BuildCorporateExecutiveSummary(GenerateKundaliResponse report)
    {
        var summary = report.Summary;
        var score = report.Scores is null ? "-" : report.Scores.Overall.ToString(CultureInfo.InvariantCulture);
        var currentPeriod = summary is null ? "-" : $"{ValueOrDash(summary.CurrentMahadasha)} / {ValueOrDash(summary.CurrentAntardasha)}";
        var lagna = ValueOrDash(summary?.Ascendant);
        var moon = ValueOrDash(summary?.MoonSign);
        var focusHouse = report.Houses?
            .OrderByDescending(h => h.Score)
            .Select(h => FirstNonEmpty(h.HouseName, $"House {h.HouseNumber}"))
            .FirstOrDefault() ?? "-";
        var sensitiveHouse = report.Houses?
            .OrderBy(h => h.Score)
            .Select(h => FirstNonEmpty(h.HouseName, $"House {h.HouseNumber}"))
            .FirstOrDefault() ?? "-";
        var narrative = $"This advisory report reads the chart from identity markers, house strength, planetary condition and timing cycles. The main orientation is {lagna} Lagna with Moon in {moon}. The strongest house theme is {focusHouse}, while {sensitiveHouse} needs more planning and conscious handling.";
        return $"<p class=\"report-text\">{Html(narrative)}</p><div class=\"grid-3\">{Fact("Overall Indicator", score)}{Fact("Current Period", currentPeriod)}{Fact("Advisory Focus", $"Use strengths in {focusHouse}; manage {sensitiveHouse}")}</div>";
    }

    private static void AppendCorporateCoreIndicators(StringBuilder html, GenerateKundaliResponse report)
    {
        var summary = report.Summary;
        var panchang = report.Panchang;
        var content = new StringBuilder("<p class=\"report-text\">The core chart indicators show the primary temperament, direction, and recurring life themes.</p><div class=\"grid-3\">");
        content.Append(Fact("Ascendant", ValueOrDash(summary?.Ascendant)));
        content.Append(Fact("Moon Sign", ValueOrDash(summary?.MoonSign)));
        content.Append(Fact("Sun Sign", ValueOrDash(summary?.SunSign)));
        content.Append("</div><div style=\"height:7px\"></div><div class=\"grid-4\">");
        content.Append(Fact("Nakshatra", ValueOrDash(summary?.Nakshatra)));
        content.Append(Fact("Tithi", ValueOrDash(panchang?.Tithi)));
        content.Append(Fact("Yoga", ValueOrDash(panchang?.Yoga)));
        content.Append(Fact("Karana", ValueOrDash(panchang?.Karana)));
        content.Append("</div>");
        AppendSection(html, "Core Chart Indicators", content.ToString());
    }

    private static void AppendCorporatePlanetaryAnalysis(StringBuilder html, List<PlanetDto>? planets)
    {
        if (planets is null || planets.Count == 0) return;
        var rows = planets.Take(18).Select(p => $"<tr><td>{Html(p.Planet)}</td><td>{Html(p.Sign)}</td><td>H{p.House}</td><td>{p.Degree}d {p.Minute:00}m</td><td>{Html(p.Nakshatra)} ({p.Pada})</td><td>{Html(PlanetStatus(p))}</td><td>{Html(ValueOrDash(p.DetailedStrength))}</td></tr>");
        AppendSection(html, "Planetary Analysis", $"<p class=\"report-text\">Planetary placements are read as indicators of temperament, decision style, resources, and areas requiring conscious management.</p><table class=\"compact-table planet-position-table\"><thead><tr><th>Planet</th><th>Sign</th><th>House</th><th>Degree</th><th>Nakshatra</th><th>Status</th><th>Strength</th></tr></thead><tbody>{string.Join(string.Empty, rows)}</tbody></table>");
    }

    private static void AppendCorporateBhavaReview(StringBuilder html, List<HouseDto>? houses, OverallScoreDto? scores)
    {
        if ((houses is null || houses.Count == 0) && scores is null) return;
        var content = new StringBuilder("<p class=\"report-text\">The scoring layer converts chart factors into comparable strength and sensitivity indicators.</p>");
        if (scores is not null)
        {
            content.Append($"<div class=\"score-strip\">{ScoreBox("Overall", scores.Overall)}{ScoreBox("Stability", scores.Stability)}{ScoreBox("Growth", scores.Growth)}{ScoreBox("Challenge", scores.Challenge)}</div><div style=\"height:8px\"></div>");
        }
        if (houses?.Count > 0)
        {
            var cards = houses.OrderBy(h => h.HouseNumber).Take(12).Select(h => $"<article class=\"bhava-card\"><div class=\"bhava-top\"><strong>H{h.HouseNumber}</strong><span>{Math.Clamp(h.Score, 0, 100)}</span></div><div class=\"score-meter\"><i style=\"width:{Math.Clamp(h.Score, 0, 100)}%\"></i></div><div class=\"bhava-meta\"><span>{Html(FirstNonEmpty(h.HouseName, h.Sign))}</span><span>{Html(FirstNonEmpty(h.HouseOwner, h.HouseSwamy, "-"))}</span></div><p>{Html(JoinCompact(h.DominantThemes, h.LifeAreas, 70))}</p><small>Observation: {Html(JoinCompact(h.Factors, h.AspectedBy, 80))}</small></article>");
            content.Append($"<div class=\"bhava-score-grid\">{string.Join(string.Empty, cards)}</div>");
        }
        AppendSection(html, "House & Bhava Strength Review", content.ToString());
    }

    private static void AppendCorporateYogasDoshas(StringBuilder html, GenerateKundaliResponse report)
    {
        var yogaItems = BuildObjectBullets(report.Yogas, "Name", "YogaName", "Description", "Reason", "Result", "Summary").Take(6).ToList();
        var doshaItems = BuildObjectBullets(report.Doshas, "Name", "DoshaName", "Description", "Reason", "Result", "Summary").Take(6).ToList();
        if (yogaItems.Count == 0 && doshaItems.Count == 0) return;
        AppendSection(html, "Yogas & Doshas", $"<p class=\"report-text\">Yogas and doshas are presented as key combinations and risk indicators that qualify the baseline chart reading.</p><div class=\"two-col-report\"><div class=\"bullet-list\">{string.Join(string.Empty, yogaItems)}</div><div class=\"bullet-list\">{string.Join(string.Empty, doshaItems)}</div></div>");
    }

    private static void AppendCorporateCurrentPeriod(StringBuilder html, CurrentDashaDto? current, List<MahadashaPeriodDto>? periods)
    {
        if (current is null && (periods is null || periods.Count == 0)) return;
        var content = new StringBuilder("<p class=\"report-text\">The timing layer identifies which parts of the chart are more active in the current period.</p>");
        if (current is not null)
        {
            content.Append($"<div class=\"grid-2\">{Fact("Mahadasha", $"{ValueOrDash(current.Mahadasha)} ({FormatDate(current.StartDate)} - {FormatDate(current.EndDate)})")}{Fact("Antardasha", $"{ValueOrDash(current.Antardasha)} ({FormatDate(current.AntardashaStartDate)} - {FormatDate(current.AntardashaEndDate)})")}</div>");
            if (!string.IsNullOrWhiteSpace(current.Summary)) content.Append($"<p class=\"report-text\">{Html(Trim(current.Summary, 550))}</p>");
        }
        if (periods?.Count > 0)
        {
            var rows = periods.Take(8).Select(p => $"<tr><td>{Html(p.Planet)}</td><td>{FormatDate(p.Start)}</td><td>{FormatDate(p.End)}</td><td>{Html(JoinCompact(p.Antardashas?.Take(4).Select(a => a.SubPlanet), null, 90))}</td></tr>");
            content.Append($"<div style=\"height:8px\"></div><table class=\"compact-table\"><thead><tr><th>Period</th><th>Start</th><th>End</th><th>Early Sub-periods</th></tr></thead><tbody>{string.Join(string.Empty, rows)}</tbody></table>");
        }
        AppendSection(html, "Current Period Analysis", content.ToString());
    }

    private static void AppendCorporateTransit(StringBuilder html, TransitInsightDto? transit)
    {
        if (transit is null) return;
        var rows = transit.Activations.Take(10).Select(a => $"<tr><td>{Html(a.Planet)}</td><td>{Html(a.Sign)}</td><td>H{a.NatalHouse}</td><td>{Html(a.FocusArea)}</td><td>{Html(Trim(a.InterpretationHint, 120))}</td></tr>");
        AppendSection(html, "Transit & Timing Outlook", $"<p class=\"report-text\">Transit indicators show near-term areas of attention when current planetary movement interacts with the natal chart.</p><div class=\"grid-2\">{Fact("Place", ValueOrDash(transit.Place))}{Fact("Natal Ascendant", ValueOrDash(transit.NatalAscendant))}</div><div style=\"height:8px\"></div><table class=\"compact-table\"><thead><tr><th>Planet</th><th>Sign</th><th>House</th><th>Focus</th><th>Guidance Hint</th></tr></thead><tbody>{string.Join(string.Empty, rows)}</tbody></table>");
    }

    private static void AppendCorporateRemedies(StringBuilder html, RemediesRecommendationDto? remedies)
    {
        if (remedies is null) return;
        var primary = remedies.Primary;
        var alt = remedies.Alternative;
        var content = $"<p class=\"report-text\">Recommendations are offered as supportive practices and should be applied with personal judgment.</p><div class=\"grid-2\">{Fact("Primary Recommendation", primary is null ? "-" : $"{FirstNonEmpty(primary.Name, primary.EnglishName, primary.RemedyType)} for {ValueOrDash(primary.Planet)}")}{Fact("Alternative", alt is null ? "-" : $"{FirstNonEmpty(alt.Name, alt.EnglishName, alt.RemedyType)} for {ValueOrDash(alt.Planet)}")}</div>";
        if (!string.IsNullOrWhiteSpace(remedies.Summary)) content += $"<p class=\"report-text\">{Html(Trim(remedies.Summary, 650))}</p>";
        AppendSection(html, "Remedial Guidance", content);
    }
    private static void AppendCoreIdentity(StringBuilder html, GenerateKundaliResponse report)
    {
        var summary = report.Summary;
        var panchang = report.Panchang;
        var content = new StringBuilder();
        content.Append("<div class=\"grid-3\">");
        content.Append(Fact("Ascendant", ValueOrDash(summary?.Ascendant)));
        content.Append(Fact("Moon Sign", ValueOrDash(summary?.MoonSign)));
        content.Append(Fact("Sun Sign", ValueOrDash(summary?.SunSign)));
        content.Append("</div><div style=\"height:7px\"></div><div class=\"grid-4\">");
        content.Append(Fact("Nakshatra", ValueOrDash(summary?.Nakshatra)));
        content.Append(Fact("Tithi", ValueOrDash(panchang?.Tithi)));
        content.Append(Fact("Yoga", ValueOrDash(panchang?.Yoga)));
        content.Append(Fact("Dasha", $"{ValueOrDash(summary?.CurrentMahadasha)} / {ValueOrDash(summary?.CurrentAntardasha)}"));
        content.Append("</div>");
        if (!string.IsNullOrWhiteSpace(summary?.Summary))
        {
            content.Append($"<p class=\"report-text\">{Html(Trim(summary.Summary, 900))}</p>");
        }

        AppendSection(html, "Core Identity & Panchang", content.ToString());
    }

    private static void AppendCharts(StringBuilder html, GenerateKundaliResponse report)
    {
        var panels = new List<string>();
        if (report.Planets?.Count > 0)
        {
            panels.Add(ChartPanel("D1 Lagna Kundali", "Natal placements", BuildPlanetHouseCells(report.Planets)));
        }

        if (report.D9Chart?.Planets?.Count > 0)
        {
            panels.Add(ChartPanel("D9 Navamsa Kundali", FirstNonEmpty(report.D9Chart.Description, report.D9Chart.Ascendant), BuildPlanetHouseCells(report.D9Chart.Planets)));
        }


        if (panels.Count == 0)
        {
            return;
        }

        AppendSection(html, "Kundali Charts", $"<div class=\"charts-grid\">{string.Join(string.Empty, panels)}</div>");
    }

    private static void AppendScores(StringBuilder html, OverallScoreDto? scores)
    {
        if (scores is null) return;
        var content = $"<div class=\"score-strip\">{ScoreBox("Overall", scores.Overall)}{ScoreBox("Stability", scores.Stability)}{ScoreBox("Growth", scores.Growth)}{ScoreBox("Challenge", scores.Challenge)}</div>";
        var strengths = JoinCompact(scores.DominantStrengths, scores.OverallFactors, 160);
        var weaknesses = JoinCompact(scores.DominantWeaknesses, scores.ChallengeFactors, 160);
        content += $"<div style=\"height:8px\"></div><div class=\"grid-2\">{Fact("Dominant Strengths", strengths)}{Fact("Dominant Weaknesses", weaknesses)}</div>";
        AppendSection(html, "Kundali Scores", content);
    }


    private static void AppendMetadataInsights(StringBuilder html, RashiInsightDto? rashi, NakshatraInsightDto? nakshatra)
    {
        if (rashi is null && nakshatra is null) return;
        var cards = new List<string>();
        if (rashi is not null)
        {
            cards.Add($"<article class=\"insight-card\"><div class=\"insight-head\"><span>Rashi</span><strong>{Html(FirstNonEmpty(rashi.EnglishName, rashi.Name))}</strong></div><div class=\"insight-rows\">{InsightRow("Symbol", rashi.Symbol)}{InsightRow("Ruler", rashi.Ruler)}{InsightRow("Element", rashi.Element)}{InsightRow("Strength", rashi.Strength)}{InsightRow("Weakness", rashi.Weakness)}{InsightRow("Gemstone", rashi.Gemstone)}{InsightRow("Metal", rashi.Metal)}{InsightRow("Color", rashi.Color)}{InsightRow("Lucky Nos.", rashi.LuckyNumbers?.Count > 0 ? string.Join(", ", rashi.LuckyNumbers) : "-")}{InsightRow("Favourable", JoinCompact(new[] { rashi.FavorableDay, rashi.FavorableDirection }, null, 80))}</div></article>");
        }

        if (nakshatra is not null)
        {
            cards.Add($"<article class=\"insight-card\"><div class=\"insight-head\"><span>Nakshatra</span><strong>{Html(ValueOrDash(nakshatra.Name))} Pada {nakshatra.Pada}</strong></div><div class=\"insight-rows\">{InsightRow("Lord", nakshatra.Lord)}{InsightRow("Symbol", nakshatra.Symbol)}{InsightRow("Gana", nakshatra.Gana)}{InsightRow("Yoni", nakshatra.Yoni)}{InsightRow("Nadi", nakshatra.Nadi)}{InsightRow("Varna", nakshatra.Varna)}{InsightRow("Vashya", nakshatra.Vashya)}{InsightRow("Dosha", nakshatra.Dosha)}{InsightRow("Fasting", nakshatra.FastingDay)}{InsightRow("Business", nakshatra.BusinessSuitability)}</div></article>");
        }

        AppendSection(html, "Rashi & Nakshatra Insights", $"<div class=\"insight-grid\">{string.Join(string.Empty, cards)}</div>");
    }

    private static void AppendBhavaScores(StringBuilder html, List<HouseDto>? houses)
    {
        if (houses is null || houses.Count == 0) return;
        var cards = houses
            .OrderBy(h => h.HouseNumber)
            .Take(12)
            .Select(h =>
            {
                var score = Math.Clamp(h.Score, 0, 100);
                var occupants = JoinCompact(h.Occupants, null, 55);
                var themes = JoinCompact(h.DominantThemes, h.LifeAreas, 70);
                var factors = JoinCompact(h.Factors, h.AspectedBy, 80);
                return $"<article class=\"bhava-card\"><div class=\"bhava-top\"><strong>H{h.HouseNumber}</strong><span>{score}</span></div><div class=\"score-meter\"><i style=\"width:{score}%\"></i></div><div class=\"bhava-meta\"><span>{Html(FirstNonEmpty(h.HouseName, h.Sign))}</span><span>{Html(FirstNonEmpty(h.HouseOwner, h.HouseSwamy, "-"))}</span></div><p>{Html(themes)}</p><small>Occupants: {Html(occupants)}</small><small>{Html(factors)}</small></article>";
            });
        AppendSection(html, "12 Bhava Score", $"<div class=\"bhava-score-grid\">{string.Join(string.Empty, cards)}</div>");
    }
    private static void AppendPlanetAndHouseTables(StringBuilder html, List<PlanetDto>? planets, List<HouseDto>? houses)
    {
        var content = new StringBuilder();
        if (planets?.Count > 0)
        {
            var planetRows = planets.Take(18).Select(p => $"<tr><td>{Html(p.Planet)}</td><td>{Html(p.Sign)}</td><td>H{p.House}</td><td>{p.Degree}d {p.Minute:00}m</td><td>{Html(p.Nakshatra)} ({p.Pada})</td><td>{Html(PlanetStatus(p))}</td><td>{Html(ValueOrDash(p.DetailedStrength))}</td></tr>");
            content.Append($"<div><h3 class=\"table-subtitle\">Planet Positions</h3><table class=\"compact-table planet-position-table\"><thead><tr><th>Planet</th><th>Sign</th><th>House</th><th>Degree</th><th>Nakshatra</th><th>Status</th><th>Strength</th></tr></thead><tbody>{string.Join(string.Empty, planetRows)}</tbody></table></div>");
        }

        if (houses?.Count > 0)
        {
            var houseRows = houses.OrderBy(h => h.HouseNumber).Take(12).Select(h => $"<tr><td>H{h.HouseNumber}</td><td>{Html(ValueOrDash(h.Sign))}</td><td>{Html(FirstNonEmpty(h.HouseOwner, h.HouseSwamy))}</td><td>{Html(JoinCompact(h.Occupants, null, 80))}</td><td>{h.Score}</td></tr>");
            content.Append($"<div><h3 class=\"table-subtitle\">House Highlights</h3><table class=\"compact-table house-highlight-table\"><thead><tr><th>House</th><th>Sign</th><th>Owner</th><th>Occupants</th><th>Score</th></tr></thead><tbody>{string.Join(string.Empty, houseRows)}</tbody></table></div>");
        }

        if (content.Length > 0)
        {
            AppendSection(html, "Planet Positions & House Highlights", $"<div class=\"report-table-stack\">{content}</div>");
        }
    }
    private static void AppendPlanetTable(StringBuilder html, List<PlanetDto>? planets)
    {
        if (planets is null || planets.Count == 0) return;
        var rows = planets.Take(18).Select(p => $"<tr><td>{Html(p.Planet)}</td><td>{Html(p.Sign)}</td><td>H{p.House}</td><td>{p.Degree}d {p.Minute:00}m</td><td>{Html(p.Nakshatra)} ({p.Pada})</td><td>{Html(PlanetStatus(p))}</td><td>{Html(ValueOrDash(p.DetailedStrength))}</td></tr>");
        AppendSection(html, "Planet Positions", $"<table class=\"compact-table\"><thead><tr><th>Planet</th><th>Sign</th><th>House</th><th>Degree</th><th>Nakshatra</th><th>Status</th><th>Strength</th></tr></thead><tbody>{string.Join(string.Empty, rows)}</tbody></table>");
    }

    private static void AppendHouses(StringBuilder html, List<HouseDto>? houses)
    {
        if (houses is null || houses.Count == 0) return;
        var rows = houses.OrderBy(h => h.HouseNumber).Take(12).Select(h => $"<tr><td>H{h.HouseNumber}</td><td>{Html(ValueOrDash(h.Sign))}</td><td>{Html(FirstNonEmpty(h.HouseOwner, h.HouseSwamy))}</td><td>{Html(JoinCompact(h.Occupants, null, 80))}</td><td>{h.Score}</td></tr>");
        AppendSection(html, "House Highlights", $"<table class=\"compact-table\"><thead><tr><th>House</th><th>Sign</th><th>Owner</th><th>Occupants</th><th>Score</th></tr></thead><tbody>{string.Join(string.Empty, rows)}</tbody></table>");
    }

    private static void AppendYogasDoshas(StringBuilder html, GenerateKundaliResponse report)
    {
        var yogaItems = BuildObjectBullets(report.Yogas, "Name", "YogaName", "Description", "Reason", "Result", "Summary").Take(6).ToList();
        var doshaItems = BuildObjectBullets(report.Doshas, "Name", "DoshaName", "Description", "Reason", "Result", "Summary").Take(6).ToList();
        if (yogaItems.Count == 0 && doshaItems.Count == 0) return;
        AppendSection(html, "Yogas & Doshas", $"<div class=\"two-col-report\"><div class=\"bullet-list\">{string.Join(string.Empty, yogaItems)}</div><div class=\"bullet-list\">{string.Join(string.Empty, doshaItems)}</div></div>");
    }

    private static void AppendDasha(StringBuilder html, CurrentDashaDto? current, List<MahadashaPeriodDto>? periods)
    {
        if (current is null && (periods is null || periods.Count == 0)) return;
        var content = new StringBuilder();
        if (current is not null)
        {
            content.Append($"<div class=\"grid-2\">{Fact("Mahadasha", $"{ValueOrDash(current.Mahadasha)} ({FormatDate(current.StartDate)} - {FormatDate(current.EndDate)})")}{Fact("Antardasha", $"{ValueOrDash(current.Antardasha)} ({FormatDate(current.AntardashaStartDate)} - {FormatDate(current.AntardashaEndDate)})")}</div>");
            if (!string.IsNullOrWhiteSpace(current.Summary)) content.Append($"<p class=\"report-text\">{Html(Trim(current.Summary, 550))}</p>");
        }
        if (periods?.Count > 0)
        {
            content.Append("<div class=\"dasha-grid\">");
            foreach (var period in periods.Take(6))
            {
                var antarRows = period.Antardashas.Select(a => $"<tr><td>{Html(a.SubPlanet)}</td><td>{FormatDate(a.StartDate)}</td><td>{FormatDate(a.EndDate)}</td></tr>");
                content.Append($"<article class=\"dasha-card\"><div class=\"dasha-head\"><strong>{Html(period.Planet)} Mahadasha</strong><span>{FormatDate(period.Start)} - {FormatDate(period.End)}</span></div><table class=\"compact-table dasha-table\"><thead><tr><th>Antardasha</th><th>Start</th><th>End</th></tr></thead><tbody>{string.Join(string.Empty, antarRows)}</tbody></table></article>");
            }
            content.Append("</div>");
        }
        AppendSection(html, "Mahadasha & Antardasha", content.ToString());
    }

    private static void AppendSudarshan(StringBuilder html, GenerateKundaliResponse report)
    {
        var sudarshan = report.SudarshanChakra;
        if (sudarshan is null) return;
        var rows = sudarshan.HouseEmphasis.OrderByDescending(h => h.TotalPlanetCount).Take(6).Select(h => $"<tr><td>H{h.HouseNumber}</td><td>{Html(h.Theme)}</td><td>{h.TotalPlanetCount}</td><td>{Html(Trim(h.Prediction, 120))}</td></tr>");
        AppendSection(html, "Sudarshan Chakra", $"<div class=\"sudarshan-large-wrap\">{SudarshanChakraSvg(report.Planets, report.Summary?.Ascendant)}</div><div style=\"height:10px\"></div><div class=\"grid-2\">{Fact("Overall Score", sudarshan.OverallScore.ToString(CultureInfo.InvariantCulture))}{Fact("Purpose", FirstNonEmpty(sudarshan.Purpose, sudarshan.Usage))}</div><p class=\"report-text\">{Html(Trim(FirstNonEmpty(sudarshan.OverallPrediction, sudarshan.CalculationMethod), 600))}</p><div style=\"height:8px\"></div><table class=\"compact-table\"><thead><tr><th>House</th><th>Theme</th><th>Total</th><th>Prediction</th></tr></thead><tbody>{string.Join(string.Empty, rows)}</tbody></table>");
    }

    private static void AppendRemedies(StringBuilder html, RemediesRecommendationDto? remedies)
    {
        if (remedies is null) return;
        var primary = remedies.Primary;
        var alt = remedies.Alternative;
        var content = new StringBuilder();
        content.Append($"<div class=\"grid-2\">{Fact("Primary", primary is null ? "-" : $"{FirstNonEmpty(primary.Name, primary.EnglishName, primary.RemedyType)} for {ValueOrDash(primary.Planet)}")}{Fact("Alternative", alt is null ? "-" : $"{FirstNonEmpty(alt.Name, alt.EnglishName, alt.RemedyType)} for {ValueOrDash(alt.Planet)}")}</div>");
        if (!string.IsNullOrWhiteSpace(remedies.Summary)) content.Append($"<p class=\"report-text\">{Html(Trim(remedies.Summary, 650))}</p>");
        AppendSection(html, "Remedies Recommendation", content.ToString());
    }

    private static void AppendTransit(StringBuilder html, TransitInsightDto? transit)
    {
        if (transit is null) return;
        var rows = transit.Activations.Take(10).Select(a => $"<tr><td>{Html(a.Planet)}</td><td>{Html(a.Sign)}</td><td>H{a.NatalHouse}</td><td>{Html(a.FocusArea)}</td><td>{Html(Trim(a.InterpretationHint, 120))}</td></tr>");
        AppendSection(html, "Transit Insight", $"<div class=\"grid-2\">{Fact("Place", ValueOrDash(transit.Place))}{Fact("Natal Ascendant", ValueOrDash(transit.NatalAscendant))}</div><div style=\"height:8px\"></div><table class=\"compact-table\"><thead><tr><th>Planet</th><th>Sign</th><th>House</th><th>Focus</th><th>Hint</th></tr></thead><tbody>{string.Join(string.Empty, rows)}</tbody></table>");
    }

    private static void AppendSection(StringBuilder html, string title, string body)
    {
        html.AppendLine($"<section class=\"report-section {SectionClass(title)}\"><h2 class=\"section-title\">{Html(title)}</h2><div class=\"section-body\">{body}</div></section>");
    }

    private static string SectionClass(string title) => "section-" + new string(title.ToLowerInvariant().Select(ch => char.IsLetterOrDigit(ch) ? ch : '-').ToArray()).Trim('-');

    private static string ChartPanel(string title, string note, string[] houses)
    {
        return $"<article class=\"chart-panel\"><div class=\"chart-title\">{Html(title)}<span>{Html(Trim(note, 56))}</span></div>{NorthChartSvg(houses)}</article>";
    }

    private static string SudarshanChartPanel(GenerateKundaliResponse report)
    {
        var score = report.SudarshanChakra?.OverallScore.ToString(CultureInfo.InvariantCulture) ?? "-";
        var houses = BuildSudarshanHouseCells(report.SudarshanChakra);
        return $"<article class=\"chart-panel sudarshan-panel\"><div class=\"chart-title\">Sudarshan Chakra<span>Overall score {Html(score)}</span></div>{SudarshanChakraSvg(report.Planets, report.Summary?.Ascendant)}{HouseLegend(houses)}</article>";
    }

    private static string NorthChartSvg(string[] houses)
    {
        var h = houses.Concat(Enumerable.Repeat("-", 12)).Take(12).Select(v => Trim(v, 20)).ToArray();
        var planetPos = new[]
        {
            (75d, 59d), (44d, 31d), (22d, 50d), (47d, 78d),
            (22d, 109d), (44d, 134d), (75d, 100d), (106d, 134d),
            (122d, 109d), (94d, 78d), (122d, 44d), (103d, 31d)
        };
        var numberPos = new[]
        {
            (75d, 69d), (44d, 40d), (34d, 47d), (63d, 82d),
            (34d, 112d), (42d, 119d), (75d, 91d), (108d, 119d),
            (116d, 112d), (88d, 79d), (116d, 51d), (105d, 39d)
        };

        var sb = new StringBuilder();
        sb.Append("<svg class=\"north-chart\" viewBox=\"0 0 156 156\" role=\"img\" aria-label=\"Kundali chart\"><rect x=\"1\" y=\"1\" width=\"154\" height=\"154\"/><line x1=\"1\" y1=\"1\" x2=\"155\" y2=\"155\"/><line x1=\"155\" y1=\"1\" x2=\"1\" y2=\"155\"/><line x1=\"78\" y1=\"1\" x2=\"155\" y2=\"78\"/><line x1=\"155\" y1=\"78\" x2=\"78\" y2=\"155\"/><line x1=\"78\" y1=\"155\" x2=\"1\" y2=\"78\"/><line x1=\"1\" y1=\"78\" x2=\"78\" y2=\"1\"/>");
        for (var i = 0; i < 12; i++)
        {
            sb.Append($"<text class=\"house-num\" x=\"{Fmt(numberPos[i].Item1)}\" y=\"{Fmt(numberPos[i].Item2)}\">{i + 1}</text>");
            if (h[i] != "-")
            {
                sb.Append(MultiLineText("planet-text", planetPos[i].Item1, planetPos[i].Item2, h[i], 3, 7.2));
            }
        }
        sb.Append("</svg>");
        return sb.ToString();
    }

    private static string SudarshanChakraSvg(IEnumerable<PlanetDto>? planets, string? ascendant)
    {
        var planetList = planets?.ToList() ?? new List<PlanetDto>();
        var lagnaSign = SignNumber(ascendant);
        var moonSign = SignNumber(planetList.FirstOrDefault(p => string.Equals(p.Planet, "Moon", StringComparison.OrdinalIgnoreCase))?.Sign);
        var sunSign = SignNumber(planetList.FirstOrDefault(p => string.Equals(p.Planet, "Sun", StringComparison.OrdinalIgnoreCase))?.Sign);
        var references = new[]
        {
            (Key: "lagna", Label: "Lagna", Reference: lagnaSign ?? 1, Outer: 145d, Inner: 108d, Text: 126d),
            (Key: "moon", Label: "Moon", Reference: moonSign ?? 1, Outer: 108d, Inner: 72d, Text: 90d),
            (Key: "sun", Label: "Sun", Reference: sunSign ?? 1, Outer: 72d, Inner: 36d, Text: 53d)
        };

        var sb = new StringBuilder();
        sb.Append("<svg class=\"sudarshan-chart-svg\" viewBox=\"0 0 320 320\" role=\"img\" aria-label=\"Sudarshan Chakra\">");
        foreach (var ring in references)
        {
            for (var house = 1; house <= 12; house++)
            {
                var start = -90 + (house - 1) * 30;
                sb.Append($"<path class=\"chakra-sector {ring.Key}-ring\" d=\"{ArcSector(start, start + 30, ring.Outer, ring.Inner)}\"/>");
            }
        }

        for (var house = 1; house <= 12; house++)
        {
            var labelPoint = PointOnCircle(-90 + (house - 1) * 30 + 15, 156);
            sb.Append($"<text class=\"chakra-house-number\" x=\"{Fmt(labelPoint.X)}\" y=\"{Fmt(labelPoint.Y)}\">H{house}</text>");
        }

        foreach (var ring in references)
        {
            var groups = planetList
                .Select(planet => new { Planet = planet, Sign = SignNumber(planet.Sign) })
                .Where(item => item.Sign is not null)
                .GroupBy(item => GetHouseFromReference(item.Sign!.Value, ring.Reference));

            foreach (var group in groups)
            {
                var angle = -90 + (group.Key - 1) * 30 + 15;
                var point = PointOnCircle(angle, ring.Text);
                var labels = string.Join(" ", group.Take(4).Select(item => PlanetShortName(item.Planet)));
                sb.Append(MultiLineText($"chakra-planet-label {ring.Key}-label", point.X, point.Y, labels, 2, 8));
            }
        }

        sb.Append("<circle class=\"chakra-center-disc\" cx=\"160\" cy=\"160\" r=\"31\"/><text class=\"chakra-center-title\" x=\"160\" y=\"156\">Sudarshan</text><text class=\"chakra-center-subtitle\" x=\"160\" y=\"168\">Chakra</text></svg>");
        return sb.ToString();
    }

    private static string MultiLineText(string className, double x, double y, string value, int maxLines, double lineHeight)
    {
        var tokens = value.Split(' ', StringSplitOptions.RemoveEmptyEntries).Take(maxLines * 2).ToArray();
        if (tokens.Length == 0)
        {
            return string.Empty;
        }

        var lines = tokens.Chunk(2).Take(maxLines).Select(chunk => Html(string.Join(" ", chunk))).ToArray();
        var firstY = y - ((lines.Length - 1) * lineHeight / 2d);
        var sb = new StringBuilder($"<text class=\"{className}\" x=\"{Fmt(x)}\" y=\"{Fmt(firstY)}\">");
        for (var i = 0; i < lines.Length; i++)
        {
            var dy = i == 0 ? 0 : lineHeight;
            sb.Append($"<tspan x=\"{Fmt(x)}\" dy=\"{Fmt(dy)}\">{lines[i]}</tspan>");
        }
        sb.Append("</text>");
        return sb.ToString();
    }

    private static string ArcSector(double startAngle, double endAngle, double outerRadius, double innerRadius)
    {
        var outerStart = PointOnCircle(startAngle, outerRadius);
        var outerEnd = PointOnCircle(endAngle, outerRadius);
        var innerEnd = PointOnCircle(endAngle, innerRadius);
        var innerStart = PointOnCircle(startAngle, innerRadius);
        return $"M {Fmt(outerStart.X)} {Fmt(outerStart.Y)} A {Fmt(outerRadius)} {Fmt(outerRadius)} 0 0 1 {Fmt(outerEnd.X)} {Fmt(outerEnd.Y)} L {Fmt(innerEnd.X)} {Fmt(innerEnd.Y)} A {Fmt(innerRadius)} {Fmt(innerRadius)} 0 0 0 {Fmt(innerStart.X)} {Fmt(innerStart.Y)} Z";
    }

    private static (double X, double Y) PointOnCircle(double angle, double radius)
    {
        var radians = angle * Math.PI / 180d;
        return (160 + radius * Math.Cos(radians), 160 + radius * Math.Sin(radians));
    }

    private static int GetHouseFromReference(int planetSign, int referenceSign) => ((planetSign - referenceSign + 12) % 12) + 1;
    private static string HouseLegend(string[] houses)
    {
        return "<div class=\"house-legend\">" + string.Join(string.Empty, houses.Take(12).Select((value, index) => $"<div><strong>H{index + 1}</strong> {Html(Trim(value, 26))}</div>")) + "</div>";
    }

    private static string[] BuildPlanetHouseCells(IEnumerable<PlanetDto>? planets)
    {
        var houses = Enumerable.Repeat("-", 12).ToArray();
        if (planets is null) return houses;
        foreach (var group in planets.Where(p => p.House >= 1 && p.House <= 12).GroupBy(p => p.House))
        {
            houses[group.Key - 1] = string.Join(" ", group.Take(5).Select(PlanetShortName));
        }
        return houses;
    }

    private static string[] BuildGocharHouseCells(TransitInsightDto? transit)
    {
        var houses = BuildPlanetHouseCells(transit?.Planets);
        if (houses.Any(h => h != "-")) return houses;
        if (transit?.Activations is null) return houses;
        foreach (var group in transit.Activations.Where(a => a.NatalHouse >= 1 && a.NatalHouse <= 12).GroupBy(a => a.NatalHouse))
        {
            houses[group.Key - 1] = string.Join(" ", group.Take(5).Select(a => PlanetShortName(a.Planet)));
        }
        return houses;
    }

    private static string[] BuildSudarshanHouseCells(SudarshanChakraDto? sudarshan)
    {
        var houses = Enumerable.Repeat("-", 12).ToArray();
        if (sudarshan?.HouseEmphasis is null) return houses;
        foreach (var item in sudarshan.HouseEmphasis.Where(h => h.HouseNumber >= 1 && h.HouseNumber <= 12))
        {
            houses[item.HouseNumber - 1] = $"T{item.TotalPlanetCount} L{item.LagnaPlanetCount} M{item.MoonPlanetCount} S{item.SunPlanetCount}";
        }
        return houses;
    }

    private static IEnumerable<string> BuildObjectBullets<T>(IEnumerable<T>? values, string nameA, string nameB, params string[] textNames)
    {
        if (values is null) yield break;
        foreach (var value in values)
        {
            var title = FirstNonEmpty(GetProperty(value!, nameA), GetProperty(value!, nameB), "Item");
            var text = FirstNonEmpty(textNames.Select(name => GetProperty(value!, name)).ToArray());
            yield return $"<article class=\"bullet-item\"><strong>{Html(title)}</strong><p>{Html(Trim(text, 220))}</p></article>";
        }
    }

    private string ReadTemplate(string fileName) => File.ReadAllText(Path.Combine(_environment.ContentRootPath, "Templates", "Reports", fileName));
    private string ReadCss(string fileName) => File.ReadAllText(Path.Combine(_environment.ContentRootPath, "Templates", "Reports", "css", fileName));

    private static string Fact(string label, string value) => $"<div class=\"fact\"><span>{Html(label)}</span><strong>{Html(ValueOrDash(value))}</strong></div>";
    private static string ScoreBox(string label, int score) => $"<div class=\"score-box\"><span>{Html(label)}</span><strong>{score}</strong></div>";
    private static string InsightRow(string label, string? value) => $"<div><span>{Html(label)}</span><strong>{Html(ValueOrDash(value))}</strong></div>";
    private static string Html(string? value) => WebUtility.HtmlEncode(value ?? string.Empty);
    private static string ValueOrDash(string? value) => string.IsNullOrWhiteSpace(value) ? "-" : value.Trim();
    private static string Trim(string? value, int max) => string.IsNullOrWhiteSpace(value) ? "-" : (value.Trim().Length <= max ? value.Trim() : value.Trim()[..Math.Max(0, max - 3)].TrimEnd() + "...");
    private static string FormatDate(DateTime value) => value == default ? "-" : value.ToString("dd MMM yyyy", CultureInfo.InvariantCulture);
    private static string BuildCoordinates(JsonElement birthProfile) => string.Join(", ", new[] { GetString(birthProfile, "latitude"), GetString(birthProfile, "longitude") }.Where(v => !string.IsNullOrWhiteSpace(v))).Trim();
    private static string PlanetStatus(PlanetDto planet) => string.Join(" ", new[] { planet.IsRetrograde ? "R" : null, planet.IsCombust ? "C" : null, ValueOrDash(planet.ConditionTone) }.Where(v => !string.IsNullOrWhiteSpace(v)));
    private static string PlanetShortName(PlanetDto planet) => PlanetShortName(planet.Planet) + (planet.IsRetrograde ? "R" : string.Empty);
    private static string PlanetShortName(string? planet) => (planet ?? string.Empty).Trim().ToLowerInvariant() switch { "sun" => "Su", "moon" => "Mo", "mars" => "Ma", "mercury" => "Me", "jupiter" => "Ju", "venus" => "Ve", "saturn" => "Sa", "rahu" => "Ra", "ketu" => "Ke", _ => string.IsNullOrWhiteSpace(planet) ? "-" : planet.Trim()[..Math.Min(2, planet.Trim().Length)] };
    private static int? SignNumber(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        var trimmed = value.Trim();
        var numberText = new string(trimmed.TakeWhile(char.IsDigit).ToArray());
        if (int.TryParse(numberText, NumberStyles.Integer, CultureInfo.InvariantCulture, out var numeric) && numeric is >= 1 and <= 12) return numeric;
        return trimmed.ToLowerInvariant() switch
        {
            "aries" or "mesha" => 1,
            "taurus" or "vrishabha" => 2,
            "gemini" or "mithuna" => 3,
            "cancer" or "karka" or "karkataka" => 4,
            "leo" or "simha" => 5,
            "virgo" or "kanya" => 6,
            "libra" or "tula" => 7,
            "scorpio" or "vrischika" or "vrishchika" => 8,
            "sagittarius" or "dhanu" => 9,
            "capricorn" or "makara" => 10,
            "aquarius" or "kumbha" => 11,
            "pisces" or "meena" => 12,
            _ => null
        };
    }
    private static string Fmt(double value) => value.ToString("0.###", CultureInfo.InvariantCulture);
    private static string JoinCompact(IEnumerable<string>? primary, IEnumerable<string>? fallback, int maxChars)
    {
        var values = primary?.Where(v => !string.IsNullOrWhiteSpace(v)).ToList() ?? new List<string>();
        if (values.Count == 0 && fallback is not null) values = fallback.Where(v => !string.IsNullOrWhiteSpace(v)).ToList();
        return values.Count == 0 ? "-" : Trim(string.Join("; ", values.Take(5)), maxChars);
    }
    private static string FirstNonEmpty(params string?[] values) => values.FirstOrDefault(v => !string.IsNullOrWhiteSpace(v))?.Trim() ?? "-";
    private static JsonElement GetObject(JsonElement element, params string[] names)
    {
        if (element.ValueKind != JsonValueKind.Object) return default;
        foreach (var name in names)
        {
            if (element.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.Object) return value;
        }
        return default;
    }
    private static string? GetString(JsonElement element, params string[] names)
    {
        if (element.ValueKind != JsonValueKind.Object) return null;
        foreach (var name in names)
        {
            if (element.TryGetProperty(name, out var value)) return value.ValueKind == JsonValueKind.String ? value.GetString() : value.ToString();
        }
        return null;
    }
    private static string? GetProperty(object value, string name)
    {
        var property = value.GetType().GetProperty(name);
        return property?.GetValue(value)?.ToString();
    }
}








