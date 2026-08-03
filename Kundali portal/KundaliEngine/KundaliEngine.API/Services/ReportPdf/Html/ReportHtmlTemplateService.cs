using System.Globalization;
using System.Net;
using System.Text;
using System.Text.Json;
using KundaliEngine.API.DTOs.Responses;

namespace KundaliEngine.API.Services.ReportPdf.Html;

public interface IReportHtmlTemplateService
{
    string BuildDesktopColorHtml(JsonElement birthProfile, GenerateKundaliResponse report);
    string BuildMobileFriendlyHtml(JsonElement birthProfile, GenerateKundaliResponse report);
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

    public string BuildMobileFriendlyHtml(JsonElement birthProfile, GenerateKundaliResponse report)
    {
        var template = ReadTemplate("kundali-report.html");
        var styles = ReadCss("base.css") + Environment.NewLine + ReadCss("desktop-color.css") + Environment.NewLine + ReadCss("mobile-friendly.css");
        var body = BuildMobileFriendlyBody(birthProfile, report);
        return template
            .Replace("{{Title}}", Html("HoraLife Mobile Friendly Kundali Report"), StringComparison.Ordinal)
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
    private string BuildMobileFriendlyBody(JsonElement birthProfile, GenerateKundaliResponse report)
    {
        var body = BuildBody(birthProfile, report, useLegacyHeader: true);
        return body
            .Replace("<main class=\"report-page\">", "<main class=\"report-page mobile-friendly-report\">", StringComparison.Ordinal)
            .Replace("<span>Desktop color theme</span>", "<span>Mobile friendly theme</span>", StringComparison.Ordinal);
    }
    private string BuildBody(JsonElement birthProfile, GenerateKundaliResponse report, bool useLegacyHeader = false)
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
        html.AppendLine(useLegacyHeader
            ? BuildLegacyReportHeader("report-hero", "HoraLife Kundali Report", "Explainable Vedic Astrology", name, birthDate, birthPlace, generatedOn, astrologerName, astrologerDesignation, astrologerAddress, astrologerContact)
            : BuildReportHeader("report-hero", "HoraLife Kundali Report", "Explainable Vedic Astrology", name, birthDate, birthTime, birthPlace, generatedOn, astrologerName, astrologerDesignation, astrologerAddress, astrologerContact));

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
        html.AppendLine(BuildReportHeader("report-hero print-bw-hero", "HoraLife Kundali Report", "Minimalist black and white print format", name, birthDate, birthTime, birthPlace, generatedOn, astrologerName, astrologerDesignation, astrologerAddress, astrologerContact));

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
        html.AppendLine(BuildReportHeader("report-hero corporate-hero", "Personal Vedic Astrology Advisory Report", "Structured birth profile, planetary indicators, timing cycles, and practical guidance", name, birthDate, birthTime, birthPlace, generatedOn, astrologerName, astrologerDesignation, astrologerAddress, astrologerContact));

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
            panels.Add(ChartPanel("D1 Lagna Kundali", "Natal placements", report.Planets, report.Summary?.Ascendant));
        }

        if (report.D9Chart?.Planets?.Count > 0)
        {
            panels.Add(ChartPanel("D9 Navamsa Kundali", FirstNonEmpty(report.D9Chart.Description, report.D9Chart.Ascendant), report.D9Chart.Planets, report.D9Chart.Ascendant));
        }


        if (report.TransitInsight?.Planets?.Count > 0)
        {
            panels.Add(ChartPanel("Gochar Kundali", BuildGocharNote(report.TransitInsight), report.TransitInsight.Planets, report.TransitInsight.NatalAscendant));
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

    private static string DevotionalDivider()
    {
        return """
<div class="devotional-divider" aria-hidden="true"><svg viewBox="0 0 760 88" xmlns="http://www.w3.org/2000/svg" role="presentation" focusable="false"><line x1="-900" y1="56" x2="309" y2="56" class="lotus-divider__line"/><line x1="451" y1="56" x2="1650" y2="56" class="lotus-divider__line"/><rect x="25.5" y="51.5" width="9" height="9" rx="0.8" transform="rotate(45 30 56)" class="lotus-divider__diamond"/><rect x="725.5" y="51.5" width="9" height="9" rx="0.8" transform="rotate(45 730 56)" class="lotus-divider__diamond"/><path d="M282 56 C294 56 301 52 307 45 C304 52 309 56 318 56" class="lotus-divider__flourish"/><path d="M478 56 C466 56 459 52 453 45 C456 52 451 56 442 56" class="lotus-divider__flourish"/><g class="lotus-divider__lotus"><path d="M380 56 C366 43 368 23 380 8 C392 23 394 43 380 56 Z"/><path d="M378 57 C361 49 351 34 355 18 C370 25 379 40 378 57 Z"/><path d="M382 57 C399 49 409 34 405 18 C390 25 381 40 382 57 Z"/><path d="M373 59 C351 57 335 45 333 29 C351 31 367 42 373 59 Z"/><path d="M387 59 C409 57 425 45 427 29 C409 31 393 42 387 59 Z"/><path d="M368 62 C343 66 322 58 312 44 C329 40 349 45 362 54 C365 56 367 59 368 62 Z"/><path d="M392 62 C417 66 438 58 448 44 C431 40 411 45 398 54 C395 56 393 59 392 62 Z"/><path d="M379 63 C361 67 346 63 336 53 C350 51 366 55 379 63 Z"/><path d="M381 63 C399 67 414 63 424 53 C410 51 394 55 381 63 Z"/></g></svg></div>
""";
    }
    private static string BuildReportHeader(
        string headerClass,
        string reportTitle,
        string reportTagline,
        string name,
        string birthDate,
        string birthTime,
        string birthPlace,
        string generatedOn,
        string astrologerName,
        string astrologerDesignation,
        string astrologerAddress,
        string astrologerContact)
    {
        return $"<header class=\"{Html(headerClass)} report-header premium-header\">" +
            "<div class=\"header-top-section\">" +
            $"<div class=\"brand-section report-brand-lockup\">{BrandLockup(reportTitle, reportTagline)}</div>" +
            $"<div class=\"astrologer-section report-prepared-by\">{PreparedBy(astrologerName, astrologerDesignation, astrologerAddress, astrologerContact)}</div>" +
            "</div>" +
            "<div class=\"header-bottom-section\"><div class=\"person-details-block\">" +
            $"<p class=\"person-detail-value\"><span class=\"intro-muted\">Prepared for</span><strong class=\"intro-name\">{Html(name)}</strong><span class=\"intro-break\"></span><span class=\"intro-muted\">Born on</span><strong class=\"intro-date\">{Html(birthDate)}</strong><span class=\"intro-muted\">at</span><strong class=\"intro-time\">{Html(birthTime)}</strong><span class=\"intro-muted\">in</span><strong class=\"intro-place\">{Html(birthPlace)}</strong></p>" +
            $"<p class=\"report-meta\">Generated {Html(generatedOn)}</p>" +
            "</div></div>" +
            DevotionalDivider() +
            "</header>";
    }

    private static string BuildLegacyReportHeader(
        string headerClass,
        string reportTitle,
        string reportTagline,
        string name,
        string birthDate,
        string birthPlace,
        string generatedOn,
        string astrologerName,
        string astrologerDesignation,
        string astrologerAddress,
        string astrologerContact)
    {
        return $"<header class=\"{Html(headerClass)}\"><div class=\"report-hero-main\"><div class=\"brand-lockup\">{BrandLockup(reportTitle, reportTagline)}</div><p class=\"subject-line\">Prepared for <strong>{Html(name)}</strong> - {Html(birthDate)} - {Html(birthPlace)}</p><p class=\"report-meta\">Generated {Html(generatedOn)}</p></div><aside class=\"prepared-by\">{PreparedBy(astrologerName, astrologerDesignation, astrologerAddress, astrologerContact)}</aside>{DevotionalDivider()}</header>";
    }

    private static string PreparedBy(string astrologerName, string astrologerDesignation, string astrologerAddress, string astrologerContact)
    {
        return $"<span class=\"prepared-label astrologer-kicker\">Prepared by</span><strong class=\"prepared-name astrologer-name\">{Html(astrologerName)}</strong><span class=\"prepared-title astrologer-title\">{Html(astrologerDesignation)}</span><span class=\"prepared-detail astrologer-contact\">{Html(astrologerContact)}</span><span class=\"prepared-detail astrologer-contact\">{Html(astrologerAddress)}</span>";
    }

    private static string BrandLockup(string reportTitle, string reportTagline)
    {
        return $"<span class=\"brand-mark report-brand-logo\">{HoraLifeLogoSvg()}</span><div class=\"brand-copy report-brand-copy\"><div class=\"brand-name report-brand-name\"><span class=\"brand-name-hora\">Hora</span><span class=\"brand-name-life\">Life</span></div><small class=\"report-brand-descriptor\">Precision Vedic Astrology</small><span class=\"report-title\">{Html(reportTitle)}</span><span class=\"report-tagline\">{Html(reportTagline)}</span></div>";
    }
    private static string HoraLifeLogoSvg()
    {
        return "<svg viewBox=\"-58 -58 116 122\" role=\"img\" aria-label=\"HoraLife logo\"><circle class=\"logo-orbit\" cx=\"-1.4\" cy=\"0\" r=\"48\"/><line class=\"logo-horizon\" x1=\"-55.2\" y1=\"-15.5\" x2=\"46.5\" y2=\"2.45\"/><circle class=\"logo-sun\" cx=\"-47.3\" cy=\"-14.1\" r=\"5.4\"/><circle class=\"logo-moon\" cx=\"33\" cy=\"-16.95\" r=\"4.5\"/><circle class=\"logo-earth\" cx=\"46.5\" cy=\"2.45\" r=\"11\"/></svg>";
    }
    private static void AppendSection(StringBuilder html, string title, string body)
    {
        html.AppendLine($"<section class=\"report-section {SectionClass(title)}\"><h2 class=\"section-title\">{Html(title)}</h2><div class=\"section-body\">{body}</div></section>");
    }

    private static string SectionClass(string title) => "section-" + new string(title.ToLowerInvariant().Select(ch => char.IsLetterOrDigit(ch) ? ch : '-').ToArray()).Trim('-');

    private static string ChartPanel(string title, string note, IEnumerable<PlanetDto>? planets, string? ascendant)
    {
        return $"<article class=\"chart-panel\"><div class=\"chart-title\">{Html(title)}<span>{Html(Trim(note, 56))}</span></div>{NorthChartSvg(planets, ascendant)}</article>";
    }

    private static string BuildGocharNote(TransitInsightDto transit)
    {
        var date = transit.CalculatedAtUtc == default
            ? string.Empty
            : transit.CalculatedAtUtc.ToString("dd MMM yyyy", CultureInfo.InvariantCulture);

        return FirstNonEmpty($"{date} {transit.Place}".Trim(), transit.LocationMode, "Current transit placements");
    }

    private static string SudarshanChartPanel(GenerateKundaliResponse report)
    {
        var score = report.SudarshanChakra?.OverallScore.ToString(CultureInfo.InvariantCulture) ?? "-";
        var houses = BuildSudarshanHouseCells(report.SudarshanChakra);
        return $"<article class=\"chart-panel sudarshan-panel\"><div class=\"chart-title\">Sudarshan Chakra<span>Overall score {Html(score)}</span></div>{SudarshanChakraSvg(report.Planets, report.Summary?.Ascendant)}{HouseLegend(houses)}</article>";
    }

    private static string NorthChartSvg(IEnumerable<PlanetDto>? planets, string? ascendant)
    {
        var planetGroups = (planets ?? Enumerable.Empty<PlanetDto>())
            .Where(p => p.House >= 1 && p.House <= 12)
            .GroupBy(p => p.House)
            .ToDictionary(g => g.Key, g => g.Take(6).ToList());
        var zodiacNumbers = ZodiacSequence(ascendant);
        var planetPos = new[]
        {
            (230d, 155d), (140d, 100d), (70d, 160d), (116d, 226d),
            (70d, 350d), (140d, 430d), (230d, 370d), (340d, 430d),
            (390d, 350d), (342d, 226d), (390d, 140d), (330d, 100d)
        };
        var numberPos = new[]
        {
            (240d, 220d), (140d, 128d), (110d, 150d), (201d, 260d),
            (110d, 360d), (136d, 380d), (240d, 290d), (345d, 380d),
            (370d, 360d), (280d, 250d), (370d, 160d), (340d, 128d)
        };

        var sb = new StringBuilder();
        sb.Append("<svg class=\"north-chart\" viewBox=\"35 35 430 430\" preserveAspectRatio=\"xMidYMid meet\" shape-rendering=\"geometricPrecision\" text-rendering=\"geometricPrecision\" role=\"img\" aria-label=\"Kundali chart\">");
        sb.Append("<rect x=\"40\" y=\"40\" width=\"420\" height=\"420\" rx=\"2\" ry=\"2\" class=\"chart-outline\"/>");
        sb.Append("<polygon points=\"250,40 460,250 250,460 40,250\" class=\"chart-secondary-outline\"/>");
        sb.Append("<path d=\"M 250 182 Q 250 180 252 182 L 318 248 Q 320 250 318 252 L 252 318 Q 250 320 248 318 L 182 252 Q 180 250 182 248 L 248 182 Q 250 180 250 182 Z\" class=\"chart-outline center-diamond\"/>");
        sb.Append("<line x1=\"250\" y1=\"40\" x2=\"250\" y2=\"460\" class=\"chart-line\"/>");
        sb.Append("<line x1=\"40\" y1=\"250\" x2=\"460\" y2=\"250\" class=\"chart-line\"/>");
        sb.Append("<line x1=\"145\" y1=\"145\" x2=\"41\" y2=\"41\" class=\"chart-line\"/>");
        sb.Append("<line x1=\"355\" y1=\"145\" x2=\"459\" y2=\"41\" class=\"chart-line\"/>");
        sb.Append("<line x1=\"355\" y1=\"355\" x2=\"459\" y2=\"459\" class=\"chart-line\"/>");
        sb.Append("<line x1=\"145\" y1=\"355\" x2=\"41\" y2=\"459\" class=\"chart-line\"/>");
        sb.Append("<line x1=\"145\" y1=\"145\" x2=\"250\" y2=\"40\" class=\"chart-line\"/>");
        sb.Append("<line x1=\"355\" y1=\"145\" x2=\"250\" y2=\"40\" class=\"chart-line\"/>");
        sb.Append("<line x1=\"355\" y1=\"355\" x2=\"460\" y2=\"250\" class=\"chart-line\"/>");
        sb.Append("<line x1=\"355\" y1=\"355\" x2=\"250\" y2=\"460\" class=\"chart-line\"/>");
        sb.Append("<line x1=\"145\" y1=\"355\" x2=\"250\" y2=\"460\" class=\"chart-line\"/>");
        sb.Append("<line x1=\"145\" y1=\"145\" x2=\"40\" y2=\"250\" class=\"chart-line\"/>");
        sb.Append("<polygon points=\"250,40 355,145 250,250 145,145\" class=\"house\"/>");
        sb.Append("<polygon points=\"145,145 250,40 41,41\" class=\"house\"/>");
        sb.Append("<polygon points=\"41,41 145,145 40,250\" class=\"house\"/>");
        sb.Append("<polygon points=\"145,145 250,250 145,355 40,250\" class=\"house\"/>");
        sb.Append("<polygon points=\"40,250 145,355 41,459\" class=\"house\"/>");
        sb.Append("<polygon points=\"145,355 250,460 41,459\" class=\"house\"/>");
        sb.Append("<polygon points=\"250,250 355,355 250,460 145,355\" class=\"house\"/>");
        sb.Append("<polygon points=\"355,355 250,460 459,459\" class=\"house\"/>");
        sb.Append("<polygon points=\"460,250 355,355 459,459\" class=\"house\"/>");
        sb.Append("<polygon points=\"355,145 460,250 355,355 250,250\" class=\"house\"/>");
        sb.Append("<polygon points=\"355,145 459,41 460,250\" class=\"house\"/>");
        sb.Append("<polygon points=\"250,40 459,41 355,145\" class=\"house\"/>");

        for (var i = 0; i < 12; i++)
        {
            sb.Append($"<text class=\"house-num\" x=\"{Fmt(numberPos[i].Item1)}\" y=\"{Fmt(numberPos[i].Item2)}\">{zodiacNumbers[i]}</text>");
            if (planetGroups.TryGetValue(i + 1, out var housePlanets))
            {
                sb.Append(PlanetLabelsForHouse(i + 1, planetPos[i].Item1, planetPos[i].Item2, housePlanets));
            }
        }

        sb.Append("</svg>");
        return sb.ToString();
    }

    private static string PlanetLabelsForHouse(int house, double x, double y, IReadOnlyList<PlanetDto> planets)
    {
        if (planets.Count == 0)
        {
            return string.Empty;
        }

        var verticalHouses = new HashSet<int> { 3, 5, 9, 11 };
        var centerSafeOffsets = CenterSafePlanetOffsets(house);
        var sb = new StringBuilder();
        for (var i = 0; i < planets.Count; i++)
        {
            var px = x;
            var py = y;
            if (centerSafeOffsets.Length > 0)
            {
                var offset = centerSafeOffsets[i % centerSafeOffsets.Length];
                px += offset.X;
                py += offset.Y + Math.Floor((double)i / centerSafeOffsets.Length) * 14d;
            }
            else if (verticalHouses.Contains(house))
            {
                py += i * 18d;
            }
            else
            {
                px += i * 18d;
            }

            var planet = planets[i];
            sb.Append($"<text class=\"planet-text {ChartPlanetToneClass(planet)}\" x=\"{Fmt(px)}\" y=\"{Fmt(py)}\">{Html(PlanetShortName(planet))}</text>");
        }

        return sb.ToString();
    }

    private static int[] ZodiacSequence(string? ascendant)
    {
        var start = SignNumber(ascendant) ?? 1;
        return Enumerable.Range(0, 12).Select(i => ((start + i - 1) % 12) + 1).ToArray();
    }

    private static string ChartPlanetToneClass(PlanetDto planet)
    {
        var tone = FirstNonEmpty(planet.ConditionTone, planet.DetailedStrength).ToLowerInvariant();
        if (tone.Contains("challenging") || tone.Contains("weak") || tone.Contains("enemy") || tone.Contains("debilitated") || planet.IsCombust)
        {
            return "tone-challenging";
        }

        if (tone.Contains("supportive") || tone.Contains("own") || tone.Contains("exalted") || tone.Contains("friendly"))
        {
            return "tone-supportive";
        }

        if (tone.Contains("mixed"))
        {
            return "tone-mixed";
        }

        return planet.IsRetrograde ? "tone-special" : "tone-neutral";
    }

    private static (double X, double Y)[] CenterSafePlanetOffsets(int house) => house switch
    {
        1 or 7 => new[] { (0d, 0d), (18d, 0d), (36d, 0d), (0d, house == 1 ? 19d : -19d), (18d, house == 1 ? 19d : -19d), (36d, house == 1 ? 19d : -19d) },
        4 => new[] { (0d, -32d), (0d, 32d), (28d, -48d), (28d, 48d), (56d, -64d), (56d, 64d) },
        10 => new[] { (0d, -32d), (0d, 32d), (-28d, -48d), (-28d, 48d), (-56d, -64d), (-56d, 64d) },
        _ => Array.Empty<(double X, double Y)>()
    };
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








