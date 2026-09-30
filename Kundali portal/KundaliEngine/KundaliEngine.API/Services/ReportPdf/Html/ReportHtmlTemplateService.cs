using System.Globalization;
using System.Net;
using System.Text;
using System.Text.Json;
using KundaliEngine.API.DTOs.Responses;

namespace KundaliEngine.API.Services.ReportPdf.Html;

public interface IReportHtmlTemplateService
{
    string BuildDesktopColorHtml(JsonElement birthProfile, GenerateKundaliResponse report, bool includePremiumReadings, IReadOnlyList<ReportLifeChapter> chapters);
    string BuildMobileFriendlyHtml(JsonElement birthProfile, GenerateKundaliResponse report, bool includePremiumReadings, IReadOnlyList<ReportLifeChapter> chapters);
    string BuildCorporateHtml(JsonElement birthProfile, GenerateKundaliResponse report, bool includePremiumReadings, IReadOnlyList<ReportLifeChapter> chapters);
    string BuildPrintBlackWhiteHtml(JsonElement birthProfile, GenerateKundaliResponse report, bool includePremiumReadings, IReadOnlyList<ReportLifeChapter> chapters);
}

public sealed class ReportHtmlTemplateService : IReportHtmlTemplateService
{
    private readonly IWebHostEnvironment _environment;
    private string? _ganeshaMarkup;

    public ReportHtmlTemplateService(IWebHostEnvironment environment)
    {
        _environment = environment;
    }

    public string BuildDesktopColorHtml(JsonElement birthProfile, GenerateKundaliResponse report, bool includePremiumReadings, IReadOnlyList<ReportLifeChapter> chapters)
    {
        return ComposeHtml(
            "HoraLife Kundali Report",
            ReadCss("base.css") + Environment.NewLine + ReadCss("desktop-color.css"),
            BuildReaderBody(birthProfile, report, includePremiumReadings, chapters, ReportSkin.Desktop));
    }

    public string BuildMobileFriendlyHtml(JsonElement birthProfile, GenerateKundaliResponse report, bool includePremiumReadings, IReadOnlyList<ReportLifeChapter> chapters)
    {
        return ComposeHtml(
            "HoraLife Mobile Friendly Kundali Report",
            ReadCss("base.css") + Environment.NewLine + ReadCss("desktop-color.css") + Environment.NewLine + ReadCss("mobile-friendly.css"),
            BuildReaderBody(birthProfile, report, includePremiumReadings, chapters, ReportSkin.Mobile));
    }

    public string BuildCorporateHtml(JsonElement birthProfile, GenerateKundaliResponse report, bool includePremiumReadings, IReadOnlyList<ReportLifeChapter> chapters)
    {
        return ComposeHtml(
            "Personal Vedic Astrology Advisory Report",
            ReadCss("base.css") + Environment.NewLine + ReadCss("corporate-advisory.css"),
            BuildReaderBody(birthProfile, report, includePremiumReadings, chapters, ReportSkin.Corporate));
    }

    public string BuildPrintBlackWhiteHtml(JsonElement birthProfile, GenerateKundaliResponse report, bool includePremiumReadings, IReadOnlyList<ReportLifeChapter> chapters)
    {
        return ComposeHtml(
            "HoraLife Kundali Report",
            ReadCss("base.css") + Environment.NewLine + ReadCss("desktop-color.css") + Environment.NewLine + ReadCss("print-black-white.css"),
            BuildReaderBody(birthProfile, report, includePremiumReadings, chapters, ReportSkin.Print));
    }

    private string ComposeHtml(string title, string styles, string body)
    {
        var template = ReadTemplate("kundali-report.html");
        return template
            .Replace("{{Title}}", Html(title), StringComparison.Ordinal)
            .Replace("{{Styles}}", styles, StringComparison.Ordinal)
            .Replace("{{Body}}", body, StringComparison.Ordinal);
    }

    private string BuildReaderBody(
        JsonElement birthProfile,
        GenerateKundaliResponse report,
        bool includePremiumReadings,
        IReadOnlyList<ReportLifeChapter> chapters,
        ReportSkin skin)
    {
        var name = ValueOrDash(BuildPersonDisplayName(birthProfile, report.Summary?.Name));
        var birthDate = ValueOrDash(GetString(birthProfile, "birthDate", "dateOfBirth"));
        var birthTime = ValueOrDash(GetString(birthProfile, "birthTime", "timeOfBirth"));
        var birthPlace = ValueOrDash(GetString(birthProfile, "birthPlace", "placeOfBirth", "birthplace"));
        var generatedOn = DateTime.Now.ToString("dd MMM yyyy, hh:mm tt", CultureInfo.InvariantCulture);
        var branding = GetObject(birthProfile, "branding", "whiteLabel", "astrologerBranding");
        var astrologerName = FirstNonEmpty(GetString(branding, "astrologerName", "name"), "Vasudev Sharna");
        var astrologerDesignation = FirstNonEmpty(GetString(branding, "designation", "role"), "Astrologer & Vastu consultant");
        var astrologerAddress = FirstNonEmpty(GetString(branding, "address"), "Ram mandir, Bhalki, Dist. Bidar 585328, Karnataka");
        var astrologerContact = FirstNonEmpty(GetString(branding, "contactNumber", "phone", "mobile"), "9035909039");
        var (mainClass, headerClass, title, tagline, footerNote) = SkinChrome(skin);

        var html = new StringBuilder();
        html.AppendLine($"<main class=\"{mainClass}\">");
        if (skin == ReportSkin.Print)
        {
            html.AppendLine($"<div class=\"print-running-header\"><span>HoraLife Kundali Report</span><strong>{Html(name)}</strong><span>{Html(generatedOn)}</span></div>");
            html.AppendLine("<div class=\"print-running-footer\"><span>Confidential - print copy</span><span>Generated by HoraLife</span><span class=\"page-counter\">Page </span></div>");
        }
        else if (skin == ReportSkin.Corporate)
        {
            html.AppendLine($"<div class=\"running-header\"><span>Personal Vedic Astrology Advisory Report</span><strong>{Html(name)}</strong><span>{Html(generatedOn)}</span></div>");
            html.AppendLine("<div class=\"running-footer\"><span>Confidential advisory report</span><span>Generated by HoraLife</span><span class=\"page-counter\">Page </span></div>");
        }

        html.AppendLine(BuildReportHeader(headerClass, title, tagline, name, birthDate, birthTime, birthPlace, generatedOn, astrologerName, astrologerDesignation, astrologerAddress, astrologerContact, birthProfile, GaneshaMark()));
        html.AppendLine($"<p class=\"cover-line\">{Html(BuildCoverSentence(report))}</p>");
        AppendContents(html, includePremiumReadings);
        AppendGlance(html, report);
        if (includePremiumReadings)
        {
            AppendLifeNow(html, report.CurrentDasha, report.MahadashaPeriods);
            AppendLifeChapters(html, chapters);
        }

        AppendCharts(html, report);
        AppendPlanetAndHouseTables(html, report.Planets, report.Houses);
        AppendYogasDoshas(html, report, namesOnly: !includePremiumReadings);
        AppendDasha(html, report.CurrentDasha, report.MahadashaPeriods, report.AdvancedStrengthTables, report.Planets);
        AppendRemedies(html, report.RemediesRecommendation);
        AppendCoreIdentity(html, report);
        AppendMetadataInsights(html, report.RashiInsight, report.NakshatraInsight);
        AppendScores(html, report.Scores);
        AppendBhavaScores(html, report.Houses);
        AppendTransit(html, report.TransitInsight);
        AppendStrength(html, report.AdvancedStrengthTables);
        if (includePremiumReadings)
        {
            AppendSudarshan(html, report);
        }

        html.AppendLine($"<footer class=\"report-footer\"><span>Confidential - generated by HoraLife</span><span>{Html(footerNote)}</span></footer>");
        html.AppendLine("</main>");
        return html.ToString();
    }

    private static (string MainClass, string HeaderClass, string Title, string Tagline, string FooterNote) SkinChrome(ReportSkin skin)
    {
        return skin switch
        {
            ReportSkin.Mobile => ("report-page mobile-friendly-report", "report-hero", "HoraLife Kundali Report", "Explainable Vedic Astrology", "Mobile friendly theme"),
            ReportSkin.Print => ("report-page print-bw-report", "report-hero print-bw-hero", "HoraLife Kundali Report", "Minimalist black and white print format", "Black & white print format"),
            ReportSkin.Corporate => ("report-page corporate-report", "report-hero corporate-hero", "Personal Vedic Astrology Advisory Report", "Structured birth profile, planetary indicators, timing cycles, and practical guidance", "Confidential advisory report"),
            _ => ("report-page", "report-hero", "HoraLife Kundali Report", "Explainable Vedic Astrology", "Desktop color theme")
        };
    }

    private enum ReportSkin
    {
        Desktop,
        Mobile,
        Print,
        Corporate
    }

    private static readonly string[] HouseMeanings =
    [
        "self and vitality",
        "savings and family",
        "courage and siblings",
        "home and study",
        "children and learning",
        "work pressure and health",
        "marriage and partnership",
        "change and longevity",
        "fortune and guidance",
        "work and reputation",
        "income and gains",
        "rest and expenses"
    ];

    private static void AppendContents(StringBuilder html, bool includePremiumReadings)
    {
        var titles = new List<string> { "At a glance" };
        if (includePremiumReadings)
        {
            titles.Add("Your life now");
            titles.Add("Life chapters");
        }

        titles.Add("Kundali Charts");
        titles.Add("Planet Positions & House Highlights");
        titles.Add("Yogas & Doshas");
        titles.Add("Mahadasha & Antardasha");
        titles.Add("Remedies");
        titles.Add("Panchang");
        titles.Add("Rashi & Nakshatra Insights");
        titles.Add("Kundali Scores");
        titles.Add("12 Bhava Score");
        titles.Add("Transit Insight");
        titles.Add("Strength Summary");
        titles.Add("Shadbala");
        titles.Add("Ashtakavarga Support");
        if (includePremiumReadings)
        {
            titles.Add("Sudarshan Chakra");
        }

        var items = string.Join(string.Empty, titles.Select(title => $"<li>{Html(title)}</li>"));
        AppendSection(html, "Contents", $"<ol class=\"report-contents\">{items}</ol>");
    }

    private static void AppendGlance(StringBuilder html, GenerateKundaliResponse report)
    {
        var strongest = report.Houses?.OrderByDescending(house => house.Score).ThenBy(house => house.HouseNumber).FirstOrDefault();
        var sensitive = report.Houses?.OrderBy(house => house.Score).ThenBy(house => house.HouseNumber).FirstOrDefault();
        var period = report.Summary is null
            ? "-"
            : $"{ValueOrDash(report.Summary.CurrentMahadasha)} / {ValueOrDash(report.Summary.CurrentAntardasha)}";
        var nextStep = strongest is null || sensitive is null
            ? "Read the current period alongside the strongest and most sensitive houses."
            : $"Use the strength in {HouseLabel(strongest.HouseNumber)}; give extra care to {HouseLabel(sensitive.HouseNumber)}.";

        var content = $"<div class=\"glance-grid\">{Fact("Strongest theme", strongest is null ? "-" : HouseLabel(strongest.HouseNumber))}{Fact("Needs care", sensitive is null ? "-" : HouseLabel(sensitive.HouseNumber))}{Fact("Current period", period)}{Fact("Next step", nextStep)}</div>";
        AppendSection(html, "At a glance", content);
    }

    private static void AppendLifeNow(StringBuilder html, CurrentDashaDto? current, List<MahadashaPeriodDto>? periods)
    {
        if (current is null && (periods is null || periods.Count == 0))
        {
            return;
        }

        var now = DateTime.UtcNow;
        var content = new StringBuilder();
        if (current is not null)
        {
            var continues = current.StartDate <= now
                ? $"The {ValueOrDash(current.Mahadasha)} Mahadasha continues until {FormatDate(current.EndDate)}"
                : $"The {ValueOrDash(current.Mahadasha)} Mahadasha runs {FormatDate(current.StartDate)} - {FormatDate(current.EndDate)}";
            var antar = current.AntardashaStartDate <= now
                ? $"with {ValueOrDash(current.Antardasha)} Antardasha continuing until {FormatDate(current.AntardashaEndDate)}"
                : $"with {ValueOrDash(current.Antardasha)} Antardasha from {FormatDate(current.AntardashaStartDate)} to {FormatDate(current.AntardashaEndDate)}";
            content.Append($"<p class=\"report-text\">{Html($"{continues}, {antar}.")}</p>");
            if (!string.IsNullOrWhiteSpace(current.Summary))
            {
                content.Append($"<p class=\"report-text\">{Html(Trim(current.Summary, 420))}</p>");
            }
        }

        var openPeriods = (periods ?? new List<MahadashaPeriodDto>())
            .Where(period => period.End > now)
            .OrderBy(period => period.Start)
            .ToList();
        if (openPeriods.Count > 0)
        {
            var rows = openPeriods.Select(period =>
            {
                var when = period.Start <= now
                    ? $"Continues until {FormatDate(period.End)}"
                    : $"{FormatDate(period.Start)} - {FormatDate(period.End)}";
                return $"<tr><td>{Html(period.Planet)}</td><td>{Html(when)}</td></tr>";
            });
            content.Append($"<table class=\"compact-table\"><thead><tr><th>Mahadasha</th><th>When</th></tr></thead><tbody>{string.Join(string.Empty, rows)}</tbody></table>");
        }

        AppendSection(html, "Your life now", content.ToString());
    }

    private static void AppendLifeChapters(StringBuilder html, IReadOnlyList<ReportLifeChapter> chapters)
    {
        if (chapters.Count == 0)
        {
            AppendSection(html, "Life chapters", "<p class=\"report-text\">Life chapters could not be prepared from this chart.</p>");
            return;
        }

        var cards = chapters.Select(chapter =>
        {
            var toneClass = chapter.Tone switch
            {
                "Supported" => "tone-supported",
                "Needs care" => "tone-needs-care",
                _ => "tone-mixed"
            };
            var window = string.IsNullOrWhiteSpace(chapter.Window)
                ? string.Empty
                : $"<p class=\"report-text life-window\">Next window: {Html(chapter.Window)}</p>";
            return $"<article class=\"life-chapter {toneClass}\"><div class=\"insight-head\"><span>{Html(chapter.Title)}</span><strong>{Html(chapter.Tone)}</strong></div><p class=\"report-text\">{Html(chapter.Summary)}</p>{window}</article>";
        });
        AppendSection(html, "Life chapters", $"<div class=\"life-chapter-grid\">{string.Join(string.Empty, cards)}</div>");
    }

    private static string BuildCoverSentence(GenerateKundaliResponse report)
    {
        var lagna = ValueOrDash(report.Summary?.Ascendant);
        var moon = ValueOrDash(report.Summary?.MoonSign);
        var period = report.Summary is null
            ? "-"
            : $"{ValueOrDash(report.Summary.CurrentMahadasha)} / {ValueOrDash(report.Summary.CurrentAntardasha)}";
        return $"This chart is read from {lagna} lagna with the Moon in {moon}. The current period is {period}.";
    }

    private static string HouseLabel(int houseNumber)
    {
        var meaning = houseNumber is >= 1 and <= 12 ? HouseMeanings[houseNumber - 1] : "life theme";
        return $"H{houseNumber}, {meaning}";
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
        content.Append(Fact("Nakshatra", FormatNakshatra(summary?.Nakshatra, panchang)));
        content.Append(Fact("Tithi", ValueOrDash(panchang?.Tithi)));
        content.Append(Fact("Yoga", ValueOrDash(panchang?.Yoga)));
        content.Append(Fact("Dasha", $"{ValueOrDash(summary?.CurrentMahadasha)} / {ValueOrDash(summary?.CurrentAntardasha)}"));
        content.Append("</div>");
        if (!string.IsNullOrWhiteSpace(summary?.Summary))
        {
            content.Append($"<p class=\"report-text\">{Html(Trim(summary.Summary, 900))}</p>");
        }

        AppendSection(html, "Panchang", content.ToString());
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
            var tara = BuildTaraRows(nakshatra.Name);
            if (!string.IsNullOrWhiteSpace(tara))
            {
                cards.Add($"<article class=\"insight-card\"><div class=\"insight-head\"><span>Tara</span><strong>Nakshatras to avoid</strong></div><div class=\"insight-rows\">{tara}</div></article>");
            }
        }

        if (rashi?.GhatChakra is { } ghat)
        {
            var moon = string.Equals(ghat.MoonMale, ghat.MoonFemale, StringComparison.OrdinalIgnoreCase) || string.IsNullOrWhiteSpace(ghat.MoonFemale)
                ? ghat.MoonMale
                : $"{ghat.MoonMale} · {ghat.MoonFemale}";
            cards.Add($"<article class=\"insight-card\"><div class=\"insight-head\"><span>Ghat Chakra</span><strong>Avoid</strong></div><div class=\"insight-rows\">{InsightRow("Month", ghat.Month)}{InsightRow("Tithi", $"{ghat.Tithi} {ghat.TithiNumbers}".Trim())}{InsightRow("Weekday", ghat.Weekday)}{InsightRow("Nakshatra", ghat.Nakshatra)}{InsightRow("Yoga", ghat.Yoga)}{InsightRow("Karana", ghat.Karana)}{InsightRow("Moon", moon)}</div></article>");
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
            var houseRows = houses.OrderBy(h => h.HouseNumber).Take(12).Select(h => $"<tr><td>{Html(HouseLabel(h.HouseNumber))}</td><td>{Html(ValueOrDash(h.Sign))}</td><td>{Html(FirstNonEmpty(h.HouseOwner, h.HouseSwamy))}</td><td>{Html(JoinCompact(h.Occupants, null, 80))}</td><td>{h.Score}</td></tr>");
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

    private static void AppendYogasDoshas(StringBuilder html, GenerateKundaliResponse report, bool namesOnly)
    {
        var yogas = report.Yogas ?? new List<YogaDto>();
        var doshas = report.Doshas ?? new List<DoshaDto>();
        if (namesOnly)
        {
            var yogaNames = yogas.Where(yoga => !string.IsNullOrWhiteSpace(yoga.Name)).Select(yoga => $"<article class=\"bullet-item\"><strong>{Html(yoga.Name)}</strong></article>");
            var doshaNames = doshas.Where(dosha => !string.IsNullOrWhiteSpace(dosha.Name)).Select(dosha => $"<article class=\"bullet-item\"><strong>{Html(dosha.Name)}</strong></article>");
            if (!yogaNames.Any() && !doshaNames.Any())
            {
                return;
            }

            AppendSection(html, "Yogas & Doshas", $"<div class=\"two-col-report\"><div class=\"bullet-list\">{string.Join(string.Empty, yogaNames)}</div><div class=\"bullet-list\">{string.Join(string.Empty, doshaNames)}</div></div>");
            return;
        }

        var yogaItems = yogas.Select(BuildYogaCard).Where(item => item.Length > 0).ToList();
        var doshaItems = doshas.Select(BuildDoshaCard).Where(item => item.Length > 0).ToList();
        if (yogaItems.Count == 0 && doshaItems.Count == 0) return;
        var focus = BuildPremiumFormationFocus(report);
        AppendSection(html, "Yogas & Doshas", $"{focus}<div class=\"two-col-report\"><div class=\"bullet-list\">{string.Join(string.Empty, yogaItems)}</div><div class=\"bullet-list\">{string.Join(string.Empty, doshaItems)}</div></div>");
    }

    private static void AppendDasha(
        StringBuilder html,
        CurrentDashaDto? current,
        List<MahadashaPeriodDto>? periods,
        AdvancedStrengthTableDto? tables,
        List<PlanetDto>? planets)
    {
        if (current is null && (periods is null || periods.Count == 0)) return;
        var scores = BuildDashaPlanetScores(tables, planets);
        var content = new StringBuilder();
        if (current is not null)
        {
            var pratyantar = FindCurrentPratyantardasha(periods);
            var pratyantarFact = pratyantar is null
                ? string.Empty
                : Fact("Pratyantardasha", $"{ValueOrDash(pratyantar.InnerPlanet)} ({FormatDate(pratyantar.StartDate)} - {FormatDate(pratyantar.EndDate)})");
            content.Append($"<div class=\"grid-2\">{Fact("Mahadasha", $"{ValueOrDash(current.Mahadasha)} ({FormatDate(current.StartDate)} - {FormatDate(current.EndDate)})")}{Fact("Antardasha", $"{ValueOrDash(current.Antardasha)} ({FormatDate(current.AntardashaStartDate)} - {FormatDate(current.AntardashaEndDate)})")}{pratyantarFact}</div>");
            if (!string.IsNullOrWhiteSpace(current.Summary)) content.Append($"<p class=\"report-text\">{Html(Trim(current.Summary, 550))}</p>");
        }
        if (periods?.Count > 0)
        {
            content.Append("<div class=\"dasha-grid\">");
            foreach (var period in periods)
            {
                var antarRows = period.Antardashas.Select(a =>
                {
                    var best = IsBestDashaPeriod(a.SubPlanet, period.Planet, scores);
                    var style = best
                        ? "background:#b7e8c6;font-weight:700;"
                        : DashaToneStyle(a.SubPlanet, period.Planet, scores);
                    var label = best ? $"{Html(a.SubPlanet)} · Highly favourable" : Html(a.SubPlanet);
                    return $"<tr><td style=\"{style}\">{label}</td><td style=\"{style}\">{FormatDate(a.StartDate)}</td><td style=\"{style}\">{FormatDate(a.EndDate)}</td></tr>";
                });
                content.Append($"<article class=\"dasha-card\"><div class=\"dasha-head\" style=\"{DashaToneStyle(period.Planet, null, scores)}\"><strong>{Html(period.Planet)} Mahadasha</strong><span>{FormatDate(period.Start)} - {FormatDate(period.End)}</span></div><table class=\"compact-table dasha-table\"><thead><tr><th>Antardasha</th><th>Start</th><th>End</th></tr></thead><tbody>{string.Join(string.Empty, antarRows)}</tbody></table></article>");
            }
            content.Append("</div>");
        }
        AppendSection(html, "Mahadasha & Antardasha", content.ToString());
    }

    private static Dictionary<string, int> BuildDashaPlanetScores(AdvancedStrengthTableDto? tables, List<PlanetDto>? planets)
    {
        var totals = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var shadbala = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        foreach (var row in tables?.PlanetaryStrengths ?? new List<PlanetaryStrengthRowDto>())
        {
            if (!string.IsNullOrWhiteSpace(row.Planet))
            {
                totals[row.Planet.Trim()] = Math.Clamp(row.TotalScore, 0, 100);
            }
        }

        foreach (var row in tables?.Shadbala ?? new List<ShadbalaRowDto>())
        {
            if (!string.IsNullOrWhiteSpace(row.Planet))
            {
                shadbala[row.Planet.Trim()] = Math.Clamp(row.Percent, 0, 100);
            }
        }

        var scores = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var planet in totals.Keys.Concat(shadbala.Keys).Distinct(StringComparer.OrdinalIgnoreCase))
        {
            var parts = new List<int>();
            if (totals.TryGetValue(planet, out var total)) parts.Add(total);
            if (shadbala.TryGetValue(planet, out var percent)) parts.Add(percent);
            scores[planet] = (int)Math.Round(parts.Average());
        }

        foreach (var planet in planets ?? new List<PlanetDto>())
        {
            if (string.IsNullOrWhiteSpace(planet.Planet) || scores.ContainsKey(planet.Planet))
            {
                continue;
            }

            var fallback = FallbackPlanetScore(planet);
            if (fallback.HasValue)
            {
                scores[planet.Planet.Trim()] = fallback.Value;
            }
        }

        return scores;
    }

    private static int? FallbackPlanetScore(PlanetDto planet)
    {
        var score = 50;
        var used = false;
        var dignity = planet.DetailedStrength ?? string.Empty;

        if (dignity.Contains("Exalted", StringComparison.OrdinalIgnoreCase) && dignity.Contains("Strong", StringComparison.OrdinalIgnoreCase))
        {
            score += 25;
            used = true;
        }
        else if (dignity.Contains("Exalted", StringComparison.OrdinalIgnoreCase))
        {
            score += 15;
            used = true;
        }
        else if (dignity.Contains("Moola", StringComparison.OrdinalIgnoreCase))
        {
            score += 20;
            used = true;
        }
        else if (dignity.Contains("Own", StringComparison.OrdinalIgnoreCase))
        {
            score += 18;
            used = true;
        }
        else if (dignity.Contains("Friend", StringComparison.OrdinalIgnoreCase))
        {
            score += 10;
            used = true;
        }
        else if (dignity.Contains("Debil", StringComparison.OrdinalIgnoreCase))
        {
            score -= 25;
            used = true;
        }
        else if (dignity.Contains("Enemy", StringComparison.OrdinalIgnoreCase))
        {
            score -= 10;
            used = true;
        }
        else if (dignity.Contains("Neutral", StringComparison.OrdinalIgnoreCase) || dignity.Contains("NotApplicable", StringComparison.OrdinalIgnoreCase))
        {
            used = true;
        }

        if (planet.House is 1 or 4 or 5 or 7 or 9 or 10)
        {
            score += 10;
            used = true;
        }
        else if (planet.House is 6 or 8 or 12)
        {
            score -= 10;
            used = true;
        }

        if (planet.IsCombust)
        {
            score -= 15;
            used = true;
        }

        if (planet.HasBeneficAspect && !planet.HasMaleficAspect)
        {
            score += 8;
            used = true;
        }
        else if (planet.HasMaleficAspect && !planet.HasBeneficAspect)
        {
            score -= 8;
            used = true;
        }
        else if (planet.HasBeneficAspect && planet.HasMaleficAspect)
        {
            used = true;
        }

        return used ? Math.Clamp(score, 0, 100) : null;
    }

    private static bool IsBestDashaPeriod(string? planet, string? companion, IReadOnlyDictionary<string, int> scores)
    {
        var primary = LookupDashaScore(planet, scores);
        var other = LookupDashaScore(companion, scores);
        return primary >= 65 && other >= 65;
    }

    private static string DashaToneStyle(string? planet, string? companion, IReadOnlyDictionary<string, int> scores)
    {
        var score = BlendDashaScores(LookupDashaScore(planet, scores), LookupDashaScore(companion, scores));
        if (score == null)
        {
            return string.Empty;
        }

        if (score >= 65)
        {
            return "background:#e7f6ec;";
        }

        return score < 50 ? "background:#fff1d6;" : string.Empty;
    }

    private static int? LookupDashaScore(string? planet, IReadOnlyDictionary<string, int> scores)
    {
        if (string.IsNullOrWhiteSpace(planet))
        {
            return null;
        }

        return scores.TryGetValue(planet.Trim(), out var score) ? score : null;
    }

    private static int? BlendDashaScores(int? primary, int? companion)
    {
        if (primary == null && companion == null)
        {
            return null;
        }

        if (primary == null)
        {
            return companion;
        }

        if (companion == null)
        {
            return primary;
        }

        return (int)Math.Round((primary.Value + companion.Value) / 2d);
    }

    private static void AppendSudarshan(StringBuilder html, GenerateKundaliResponse report)
    {
        var sudarshan = report.SudarshanChakra;
        if (sudarshan is null) return;
        var rows = sudarshan.HouseEmphasis.OrderByDescending(h => h.TotalPlanetCount).Select(h => $"<tr><td>H{h.HouseNumber}</td><td>{Html(h.Theme)}</td><td>{h.TotalPlanetCount}</td><td>{Html(Trim(h.Prediction, 160))}</td></tr>");
        var interpretations = sudarshan.Interpretations
            .Select(item => $"<article class=\"insight-card\"><div class=\"insight-head\"><span>{Html(item.Area)}</span><strong>{item.Score}/100</strong></div><p class=\"report-text\">{Html(ValueOrDash(item.Tone))}</p><p class=\"report-text\">{Html(Trim(item.Prediction, 220))}</p></article>");
        var interpretationGrid = sudarshan.Interpretations.Count == 0
            ? string.Empty
            : $"<div class=\"insight-grid\">{string.Join(string.Empty, interpretations)}</div><div style=\"height:8px\"></div>";
        AppendSection(html, "Sudarshan Chakra", $"<div class=\"sudarshan-large-wrap\">{SudarshanChakraSvg(report.Planets, report.Summary?.Ascendant)}</div><div style=\"height:10px\"></div><div class=\"grid-2\">{Fact("Overall Score", sudarshan.OverallScore.ToString(CultureInfo.InvariantCulture))}{Fact("Purpose", FirstNonEmpty(sudarshan.Purpose, sudarshan.Usage))}</div><p class=\"report-text\">{Html(Trim(FirstNonEmpty(sudarshan.OverallPrediction, sudarshan.CalculationMethod), 600))}</p>{interpretationGrid}<table class=\"compact-table\"><thead><tr><th>House</th><th>Theme</th><th>Total</th><th>Prediction</th></tr></thead><tbody>{string.Join(string.Empty, rows)}</tbody></table>");
    }

    private static void AppendRemedies(StringBuilder html, RemediesRecommendationDto? remedies)
    {
        if (remedies is null) return;
        var content = new StringBuilder();
        var primary = remedies.Primary;
        var alt = remedies.Alternative;
        content.Append("<div class=\"grid-3\">");
        content.Append(Fact("Primary", primary is null ? "-" : $"{FirstNonEmpty(primary.Name, primary.EnglishName, primary.RemedyType)} for {ValueOrDash(primary.Planet)}"));
        content.Append(Fact("Suitability score", primary is null ? "-" : $"{primary.Confidence}/100"));
        content.Append(Fact("Suitability", primary is null ? "-" : ValueOrDash(primary.Suitability)));
        content.Append("</div>");
        if (alt is not null)
        {
            content.Append($"<div style=\"height:8px\"></div><div class=\"grid-3\">{Fact("Alternative", $"{FirstNonEmpty(alt.Name, alt.EnglishName, alt.RemedyType)} for {ValueOrDash(alt.Planet)}")}{Fact("Alternative score", $"{alt.Confidence}/100")}{Fact("Strength", ValueOrDash(FirstNonEmpty(primary?.StrengthLevel, alt.StrengthLevel)))}</div>");
        }

        if (!string.IsNullOrWhiteSpace(primary?.Reason))
        {
            content.Append($"<p class=\"report-text\">{Html(Trim(primary.Reason, 500))}</p>");
        }

        content.Append(BuildRemedyCatalog(primary));

        if (remedies.Categories.Count > 0)
        {
            var rows = remedies.Categories.Select(category => $"<tr><td>{Html(category.Category)}</td><td>{Html(ValueOrDash(category.RecommendedRemedy))}</td><td>{Html(ValueOrDash(category.Planet))}</td><td>{category.Score}/100</td><td>{Html(ValueOrDash(category.Suitability))}</td></tr>");
            content.Append($"<h3 class=\"table-subtitle\">Remedy scores</h3><table class=\"compact-table\"><thead><tr><th>Area</th><th>Remedy</th><th>Planet</th><th>Score</th><th>Suitability</th></tr></thead><tbody>{string.Join(string.Empty, rows)}</tbody></table>");
        }

        if (remedies.Avoid.Count > 0)
        {
            var rows = remedies.Avoid.Select(item => $"<tr><td>{Html(ValueOrDash(item.RemedyName))}</td><td>{Html(ValueOrDash(item.Planet))}</td><td>{Html(Trim(item.Reason, 180))}</td></tr>");
            content.Append($"<h3 class=\"table-subtitle\">Cautions</h3><table class=\"compact-table\"><thead><tr><th>Remedy</th><th>Planet</th><th>Reason</th></tr></thead><tbody>{string.Join(string.Empty, rows)}</tbody></table>");
        }

        if (!string.IsNullOrWhiteSpace(remedies.Summary)) content.Append($"<p class=\"report-text\">{Html(Trim(remedies.Summary, 650))}</p>");
        if (!string.IsNullOrWhiteSpace(remedies.Disclaimer)) content.Append($"<p class=\"report-text\">{Html(Trim(remedies.Disclaimer, 300))}</p>");
        AppendSection(html, "Remedies", content.ToString());
    }

    private static string BuildRemedyCatalog(RemedySelectionDto? selection)
    {
        var catalog = selection?.Remedies;
        if (catalog is null)
        {
            return string.Empty;
        }

        var parts = new List<string>();
        var gem = catalog.Gemstone;
        if (!string.IsNullOrWhiteSpace(gem?.Primary?.Name) || !string.IsNullOrWhiteSpace(gem?.WearingRules?.Finger))
        {
            var rules = gem?.WearingRules;
            parts.Add($"<div class=\"grid-4\">{Fact("Gemstone", ValueOrDash(gem?.Primary?.Name))}{Fact("Finger", ValueOrDash(rules?.Finger))}{Fact("Day", ValueOrDash(rules?.DayToWear))}{Fact("Metal", JoinCompact(rules?.Metal, null, 40))}</div>");
            if (gem?.Alternatives is { Count: > 0 })
            {
                parts.Add($"<p class=\"report-text\">Substitutes: {Html(string.Join(", ", gem.Alternatives.Select(stone => stone.Name).Where(name => !string.IsNullOrWhiteSpace(name))))}</p>");
            }
        }

        if (!string.IsNullOrWhiteSpace(catalog.Yantra?.Name))
        {
            parts.Add($"<p class=\"report-text\"><strong>Yantra.</strong> {Html(catalog.Yantra.Name)}. {Html(Trim(catalog.Yantra.Purpose, 180))}</p>");
        }

        if (!string.IsNullOrWhiteSpace(catalog.Japa?.Mantra) || catalog.Daan.Count > 0)
        {
            var daan = catalog.Daan.Count == 0 ? string.Empty : " Daan: " + string.Join(", ", catalog.Daan);
            parts.Add($"<p class=\"report-text\"><strong>Japa.</strong> {Html(ValueOrDash(catalog.Japa?.Mantra))}{Html(daan)}</p>");
        }

        if (!string.IsNullOrWhiteSpace(gem?.Caution))
        {
            parts.Add($"<p class=\"report-text\">{Html(Trim(gem.Caution, 240))}</p>");
        }

        return string.Join(string.Empty, parts);
    }

    private static void AppendTransit(StringBuilder html, TransitInsightDto? transit)
    {
        if (transit is null) return;
        var rows = transit.Activations.Take(10).Select(a => $"<tr><td>{Html(a.Planet)}</td><td>{Html(a.Sign)}</td><td>H{a.NatalHouse}</td><td>{Html(a.FocusArea)}</td><td>{Html(Trim(a.InterpretationHint, 120))}</td></tr>");
        AppendSection(html, "Transit Insight", $"<div class=\"grid-2\">{Fact("Place", ValueOrDash(transit.Place))}{Fact("Natal Ascendant", ValueOrDash(transit.NatalAscendant))}</div><div style=\"height:8px\"></div><table class=\"compact-table\"><thead><tr><th>Planet</th><th>Sign</th><th>House</th><th>Focus</th><th>Hint</th></tr></thead><tbody>{string.Join(string.Empty, rows)}</tbody></table>");
    }

    private string GaneshaMark()
    {
        if (_ganeshaMarkup is not null)
        {
            return _ganeshaMarkup;
        }

        var path = Path.Combine(_environment.ContentRootPath, "Templates", "Reports", "images", "ganesh-light-clean.png");
        if (!File.Exists(path))
        {
            _ganeshaMarkup = string.Empty;
            return _ganeshaMarkup;
        }

        var image = "data:image/png;base64," + Convert.ToBase64String(File.ReadAllBytes(path));
        _ganeshaMarkup = $"<div class=\"report-header-deity\"><img class=\"ganesha-icon\" src=\"{image}\" alt=\"\" /></div>";
        return _ganeshaMarkup;
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
        string astrologerContact,
        JsonElement birthProfile,
        string ganeshaMark)
    {
        var familyLine = BuildFamilyHeaderLine(birthProfile);
        return $"<header class=\"{Html(headerClass)} report-header premium-header\">" +
            "<div class=\"header-top-section\">" +
            $"<div class=\"brand-section report-brand-lockup\">{BrandLockup(reportTitle, reportTagline)}</div>" +
            $"<div class=\"astrologer-section report-prepared-by\">{PreparedBy(astrologerName, astrologerDesignation, astrologerAddress, astrologerContact)}</div>" +
            "</div>" +
            "<div class=\"header-bottom-section\"><div class=\"person-details-block\">" +
            $"<p class=\"person-detail-value\"><span class=\"intro-muted\">Prepared for</span><strong class=\"intro-name\">{Html(name)}</strong><span class=\"intro-break\"></span><span class=\"intro-muted\">Born on</span><strong class=\"intro-date\">{Html(birthDate)}</strong><span class=\"intro-muted\">at</span><strong class=\"intro-time\">{Html(birthTime)}</strong><span class=\"intro-muted\">in</span><strong class=\"intro-place\">{Html(birthPlace)}</strong></p>" +
            familyLine +
            $"<p class=\"report-meta\">Generated {Html(generatedOn)}</p>" +
            "</div></div>" +
            ganeshaMark +
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
    private static readonly string[] TaraStars =
    [
        "Ashwini", "Bharani", "Krittika", "Rohini", "Mrigashira", "Ardra", "Punarvasu", "Pushya", "Ashlesha",
        "Magha", "Purva Phalguni", "Uttara Phalguni", "Hasta", "Chitra", "Swati", "Vishakha", "Anuradha", "Jyeshtha",
        "Mula", "Purva Ashadha", "Uttara Ashadha", "Shravana", "Dhanishta", "Shatabhisha", "Purva Bhadrapada", "Uttara Bhadrapada", "Revati"
    ];

    private static string FormatNakshatra(string? name, PanchangDto? panchang)
    {
        var label = ValueOrDash(name);
        if (panchang is null)
        {
            return label;
        }

        var pada = panchang.Pada > 0 ? $" Pada {panchang.Pada}" : string.Empty;
        var letter = string.IsNullOrWhiteSpace(panchang.Akshara) ? string.Empty : $" {panchang.Akshara}";
        return $"{label}{pada}{letter}".Trim();
    }

    private static string BuildTaraRows(string? name)
    {
        var index = FindNakshatraIndex(name);
        if (index < 0)
        {
            return string.Empty;
        }

        string Stars(int offset) => string.Join(", ", new[] { 0, 9, 18 }.Select(step => TaraStars[(index + offset + step) % 27]));
        return InsightRow("Vipat", Stars(2)) + InsightRow("Pratyari", Stars(4)) + InsightRow("Vadha", Stars(6));
    }

    private static int FindNakshatraIndex(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return -1;
        }

        var key = new string(name.Where(char.IsLetter).ToArray()).ToLowerInvariant();
        key = key.Replace("moola", "mula", StringComparison.Ordinal)
            .Replace("ashada", "ashadha", StringComparison.Ordinal)
            .Replace("phalguni", "phalguni", StringComparison.Ordinal);
        for (var i = 0; i < TaraStars.Length; i++)
        {
            var star = new string(TaraStars[i].Where(char.IsLetter).ToArray()).ToLowerInvariant();
            if (key == star || key.Contains(star, StringComparison.Ordinal) || star.Contains(key, StringComparison.Ordinal))
            {
                return i;
            }
        }

        return -1;
    }

    private static string BuildPremiumFormationFocus(GenerateKundaliResponse report)
    {
        var hasTiming = (report.Doshas ?? new List<DoshaDto>()).Any(item => item.ActivationPeriods is not null)
            || (report.Yogas ?? new List<YogaDto>()).Any(item => item.ActivationPeriods is not null);
        if (!hasTiming)
        {
            return string.Empty;
        }

        var doshas = RankDoshaAttention(report.Doshas ?? new List<DoshaDto>());
        var yogas = RankYogaInfluence(report.Yogas ?? new List<YogaDto>());
        if (doshas.Count == 0 && yogas.Count == 0)
        {
            return string.Empty;
        }

        var cards = new StringBuilder("<div class=\"formation-focus\">");
        if (doshas.Count > 0)
        {
            var focus = doshas[0];
            var also = string.Join(", ", doshas.Skip(1).Take(2).Select(item => item.Name));
            var remedies = focus.Remedies.Count == 0
                ? string.Empty
                : $"<p class=\"report-text\">Start remedies with: {Html(string.Join("; ", focus.Remedies.Take(3)))}</p>";
            var alsoLine = string.IsNullOrWhiteSpace(also) ? string.Empty : $"<p class=\"report-text\">Also watch: {Html(also)}</p>";
            cards.Append($"<article class=\"insight-card\"><div class=\"insight-head\"><span>Needs attention</span><strong>{Html(focus.Name)}</strong></div><p class=\"report-text\">{Html(focus.Analysis)}</p>{remedies}{alsoLine}</article>");
        }

        if (yogas.Count > 0)
        {
            var focus = yogas[0];
            var also = string.Join(", ", yogas.Skip(1).Take(2).Select(item => item.Name));
            var alsoLine = string.IsNullOrWhiteSpace(also) ? string.Empty : $"<p class=\"report-text\">Also supportive now: {Html(also)}</p>";
            cards.Append($"<article class=\"insight-card\"><div class=\"insight-head\"><span>Most influential yoga</span><strong>{Html(focus.Name)}</strong></div><p class=\"report-text\">{Html(focus.Analysis)}</p>{alsoLine}</article>");
        }

        cards.Append("</div>");
        return cards.ToString();
    }

    private static List<FormationFocus> RankDoshaAttention(List<DoshaDto> doshas)
    {
        return doshas
            .Select(ScoreDosha)
            .Where(item => item is not null && item.Kind != "quiet")
            .Select(item => item!)
            .OrderByDescending(item => item.Score)
            .ThenBy(item => item.Name, StringComparer.Ordinal)
            .ToList();
    }

    private static List<FormationFocus> RankYogaInfluence(List<YogaDto> yogas)
    {
        return yogas
            .Select(ScoreYoga)
            .Where(item => item is not null && item.Kind != "quiet")
            .Select(item => item!)
            .OrderByDescending(item => item.Score)
            .ThenBy(item => item.Name, StringComparer.Ordinal)
            .ToList();
    }

    private static FormationFocus? ScoreDosha(DoshaDto dosha)
    {
        if (string.IsNullOrWhiteSpace(dosha.Name))
        {
            return null;
        }

        var severity = ReadBand(dosha.Severity, "high", "medium");
        var current = dosha.ActivationPeriods?.CurrentlyActive?.FirstOrDefault();
        var upcoming = dosha.ActivationPeriods?.Upcoming?.FirstOrDefault();
        var currentLevel = ReadLevel(current);
        var score = severity == "high" ? 36 : severity == "medium" ? 18 : 6;
        var kind = "quiet";
        var lines = new List<string>();

        if (severity == "high")
        {
            lines.Add("The birth chart carries this dosha at high strength, so it can affect life beyond a single dasha.");
        }
        else if (severity == "medium" && (currentLevel == "strong" || currentLevel == "active"))
        {
            lines.Add("It is also present in the birth chart at moderate strength.");
        }

        if (currentLevel == "strong")
        {
            score += 48;
            kind = "strong";
            lines.Insert(0, $"Both {current!.MahadashaPlanet} Mahadasha and {current.AntardashaPlanet} Antardasha belong to this dosha, so it is strongest from {FormatDate(current.StartDate)} to {FormatDate(current.EndDate)}.");
        }
        else if (currentLevel == "active")
        {
            score += 28;
            kind = "period";
            lines.Insert(0, $"{current!.MahadashaPlanet} Mahadasha belongs to this dosha, so it is pressing from {FormatDate(current.StartDate)} to {FormatDate(current.EndDate)}. The antardasha planet is outside this dosha.");
        }
        else if (currentLevel == "mild" && severity == "high")
        {
            score += 10;
            kind = "period";
            lines.Insert(0, $"Only {current!.AntardashaPlanet} Antardasha is involved now, so the current push is lighter than a full activation.");
        }
        else if (severity == "high")
        {
            kind = "lifetime";
            var upcomingLevel = ReadLevel(upcoming);
            if (upcoming is not null && (upcomingLevel == "strong" || upcomingLevel == "active"))
            {
                lines.Add($"A stronger window returns {FormatDate(upcoming.StartDate)} to {FormatDate(upcoming.EndDate)}.");
            }
        }

        if (kind == "quiet")
        {
            return new FormationFocus(dosha.Name, kind, score, string.Empty, new List<string>());
        }

        return new FormationFocus(dosha.Name, kind, score, string.Join(" ", lines), (dosha.Remedies ?? new List<string>()).Where(item => !string.IsNullOrWhiteSpace(item)).Take(3).ToList());
    }

    private static FormationFocus? ScoreYoga(YogaDto yoga)
    {
        if (string.IsNullOrWhiteSpace(yoga.Name))
        {
            return null;
        }

        var strength = ReadBand(yoga.Strength, "strong", "medium");
        if (strength == "low" && (yoga.Strength ?? string.Empty).Contains("neutral", StringComparison.OrdinalIgnoreCase))
        {
            strength = "medium";
        }

        var current = yoga.ActivationPeriods?.CurrentlyActive?.FirstOrDefault();
        var currentLevel = ReadLevel(current);
        var score = strength == "high" ? 36 : strength == "medium" ? 18 : 6;
        var kind = "quiet";
        var lines = new List<string>();
        var impact = Trim(FirstNonEmpty(yoga.Impact, yoga.Description, yoga.Reason), 280);
        if (!string.IsNullOrWhiteSpace(impact))
        {
            lines.Add(impact);
        }

        if (currentLevel == "strong")
        {
            score += 48;
            kind = "strong";
            lines.Insert(0, $"Both {current!.MahadashaPlanet} Mahadasha and {current.AntardashaPlanet} Antardasha belong to this yoga, so its influence is strongest from {FormatDate(current.StartDate)} to {FormatDate(current.EndDate)}.");
        }
        else if (currentLevel == "active")
        {
            score += 28;
            kind = "period";
            lines.Insert(0, $"{current!.MahadashaPlanet} Mahadasha belongs to this yoga, so it is active from {FormatDate(current.StartDate)} to {FormatDate(current.EndDate)}.");
        }
        else if (strength == "high")
        {
            kind = "lifetime";
            lines.Insert(0, "This yoga is strong in the birth chart, so it shapes life even when the current dasha is not triggering it.");
        }

        if (kind == "quiet")
        {
            return new FormationFocus(yoga.Name, kind, score, string.Empty, new List<string>());
        }

        return new FormationFocus(yoga.Name, kind, score, string.Join(" ", lines), new List<string>());
    }

    private static string ReadBand(string? value, string highWord, string mediumWord)
    {
        var text = value ?? string.Empty;
        if (text.Contains(highWord, StringComparison.OrdinalIgnoreCase) || text.Contains("high", StringComparison.OrdinalIgnoreCase))
        {
            return "high";
        }

        if (text.Contains(mediumWord, StringComparison.OrdinalIgnoreCase) || text.Contains("moderate", StringComparison.OrdinalIgnoreCase))
        {
            return "medium";
        }

        return "low";
    }

    private static string ReadLevel(YogaDoshaActivationDto? activation)
    {
        var level = activation?.ActivationLevel ?? string.Empty;
        if (level.Equals("strong", StringComparison.OrdinalIgnoreCase)
            || level.Equals("active", StringComparison.OrdinalIgnoreCase)
            || level.Equals("mild", StringComparison.OrdinalIgnoreCase))
        {
            return level.ToLowerInvariant();
        }

        return string.Empty;
    }

    private static PratyantardashaPeriodDto? FindCurrentPratyantardasha(List<MahadashaPeriodDto>? periods)
    {
        if (periods is null)
        {
            return null;
        }

        var now = DateTime.UtcNow;
        foreach (var mahadasha in periods)
        {
            if (now < mahadasha.Start || now >= mahadasha.End)
            {
                continue;
            }

            foreach (var antardasha in mahadasha.Antardashas ?? new List<AntardashaPeriodDto>())
            {
                if (now < antardasha.StartDate || now >= antardasha.EndDate)
                {
                    continue;
                }

                return antardasha.Pratyantardashas?.FirstOrDefault(period => now >= period.StartDate && now < period.EndDate);
            }
        }

        return null;
    }

    private sealed record FormationFocus(string Name, string Kind, int Score, string Analysis, List<string> Remedies);

    private static string BuildYogaCard(YogaDto yoga)
    {
        if (string.IsNullOrWhiteSpace(yoga.Name))
        {
            return string.Empty;
        }

        var meta = string.Join(" · ", new[]
        {
            yoga.Category,
            yoga.InvolvedPlanets?.Count > 0 ? string.Join(", ", yoga.InvolvedPlanets) : string.Empty,
            yoga.InvolvedHouses?.Count > 0 ? "Houses " + string.Join(", ", yoga.InvolvedHouses) : string.Empty,
            yoga.Strength
        }.Where(value => !string.IsNullOrWhiteSpace(value)));
        var text = FirstNonEmpty(yoga.Description, yoga.Reason, yoga.Impact);
        return $"<article class=\"insight-card\"><div class=\"insight-head\"><span>Yoga</span><strong>{Html(yoga.Name)}</strong></div><p class=\"report-text\">{Html(meta)}</p><p class=\"report-text\">{Html(Trim(text, 500))}</p>{ActivationLines(yoga.ActivationPeriods)}{ImpactLines(yoga.Impact, yoga.ImpactAreas)}</article>";
    }

    private static string BuildDoshaCard(DoshaDto dosha)
    {
        if (string.IsNullOrWhiteSpace(dosha.Name))
        {
            return string.Empty;
        }

        var meta = string.Join(" · ", new[]
        {
            dosha.Category,
            dosha.Severity,
            dosha.InvolvedPlanets?.Count > 0 ? string.Join(", ", dosha.InvolvedPlanets) : string.Empty
        }.Where(value => !string.IsNullOrWhiteSpace(value)));
        return $"<article class=\"insight-card\"><div class=\"insight-head\"><span>Dosha</span><strong>{Html(dosha.Name)}</strong></div><p class=\"report-text\">{Html(meta)}</p><p class=\"report-text\">{Html(Trim(FirstNonEmpty(dosha.Description, dosha.Reason), 500))}</p>{ActivationLines(dosha.ActivationPeriods)}{DoshaImpactLines(dosha)}</article>";
    }

    private static string ActivationLines(FormationActivationPeriodsDto? periods)
    {
        if (periods is null)
        {
            return string.Empty;
        }

        var rows = periods.CurrentlyActive.Concat(periods.Upcoming).Take(6).Select(item =>
        {
            var strong = string.Equals(item.ActivationLevel, "Strong", StringComparison.OrdinalIgnoreCase) ? "Strong influence" : item.ActivationLevel;
            return $"<li>{Html(item.IsCurrentlyActive ? "Current" : "Upcoming")} {FormatDate(item.StartDate)} - {FormatDate(item.EndDate)} · {Html(item.MahadashaPlanet)} Mahadasha / {Html(item.AntardashaPlanet)} Antardasha · {Html(strong)}</li>";
        });
        var list = string.Join(string.Empty, rows);
        return string.IsNullOrWhiteSpace(list) ? string.Empty : $"<ul>{list}</ul>";
    }

    private static string ImpactLines(string? impact, List<YogaImpactAreaDto>? areas)
    {
        var parts = new List<string>();
        if (!string.IsNullOrWhiteSpace(impact))
        {
            parts.Add($"<p class=\"report-text\">{Html(Trim(impact, 400))}</p>");
        }

        if (areas is { Count: > 0 })
        {
            parts.Add(string.Join(string.Empty, areas.Take(4).Select(area => $"<p class=\"report-text\"><strong>{Html(area.Category)}.</strong> {Html(Trim(string.Join(" ", area.Statements ?? new List<string>()), 240))}</p>")));
        }

        return string.Join(string.Empty, parts);
    }

    private static string DoshaImpactLines(DoshaDto dosha)
    {
        if (dosha.ImpactAreas is not { Count: > 0 } && dosha.Remedies is not { Count: > 0 })
        {
            return string.Empty;
        }

        var areas = dosha.ImpactAreas?.Take(4).Select(area => $"<p class=\"report-text\"><strong>{Html(area.Category)}.</strong> {Html(Trim(string.Join(" ", area.Statements ?? new List<string>()), 240))}</p>") ?? Array.Empty<string>();
        var remedies = dosha.Remedies is { Count: > 0 } ? $"<p class=\"report-text\">Remedies: {Html(string.Join("; ", dosha.Remedies.Take(4)))}</p>" : string.Empty;
        return string.Join(string.Empty, areas) + remedies;
    }

    private static void AppendStrength(StringBuilder html, AdvancedStrengthTableDto? tables)
    {
        if (tables is null)
        {
            return;
        }

        if (tables.PlanetaryStrengths?.Count > 0)
        {
            var rows = tables.PlanetaryStrengths.Select(row => $"<tr><td>{Html(row.Planet)}</td><td>{row.TotalScore}/100</td><td>{Html(row.StrengthLevel)}</td><td>{Html(row.SthanaBala)}</td><td>{Html(row.DigBala)}</td><td>{Html(row.KalaBala)}</td><td>{Html(row.CheshtaBala)}</td><td>{Html(row.NaisargikaBala)}</td><td>{Html(row.DrikBala)}</td><td>{Html(Trim(row.Interpretation, 120))}</td></tr>");
            AppendSection(html, "Strength Summary", $"<table class=\"compact-table\"><thead><tr><th>Planet</th><th>Total</th><th>Level</th><th>Sthana</th><th>Dig</th><th>Kala</th><th>Cheshta</th><th>Naisargika</th><th>Drik</th><th>Reading</th></tr></thead><tbody>{string.Join(string.Empty, rows)}</tbody></table>");
        }

        if (tables.Shadbala?.Count > 0)
        {
            var rows = tables.Shadbala.Select(row => $"<tr><td>{Html(row.Planet)}</td><td>{row.SthanaBala:0.#}</td><td>{row.DigBala:0.#}</td><td>{row.KalaBala:0.#}</td><td>{row.CheshtaBala:0.#}</td><td>{row.NaisargikaBala:0.#}</td><td>{row.DrikBala:0.#}</td><td>{row.Total:0.#}</td><td>{row.Required:0.#}</td><td>{row.Percent}%</td><td>{Html(row.StrengthLevel)}</td></tr>");
            AppendSection(html, "Shadbala", $"<table class=\"compact-table\"><thead><tr><th>Planet</th><th>Sthana</th><th>Dig</th><th>Kala</th><th>Cheshta</th><th>Naisargika</th><th>Drik</th><th>Total</th><th>Required</th><th>%</th><th>Result</th></tr></thead><tbody>{string.Join(string.Empty, rows)}</tbody></table>");
        }

        if (tables.HouseSupport?.Count > 0)
        {
            var rows = tables.HouseSupport.OrderBy(row => row.House).Select(row => $"<tr><td>H{row.House}</td><td>{Html(row.Sign)}</td><td>{row.EstimatedBavPoints}/8</td><td>{row.EstimatedSavPoints}</td><td>{row.Score}/100</td><td>{Html(row.SupportLevel)}</td><td>{Html(Trim(row.LifeAreas, 80))}</td><td>{Html(Trim(row.TransitUsefulness, 80))}</td></tr>");
            AppendSection(html, "Ashtakavarga Support", $"<table class=\"compact-table\"><thead><tr><th>House</th><th>Sign</th><th>BAV</th><th>SAV</th><th>Score</th><th>Support</th><th>Life areas</th><th>Transit usefulness</th></tr></thead><tbody>{string.Join(string.Empty, rows)}</tbody></table>");
        }
    }

    private static void AppendPredictions(StringBuilder html, GenerateKundaliResponse report)
    {
        if (report.Predictions is not { Count: > 0 })
        {
            return;
        }

        var cards = report.Predictions.Select(item =>
        {
            var tone = ValueOrDash(item.Synthesis?.OverallTone ?? item.Answer?.Outcome);
            var claims = item.Claims?.Take(4).Select(claim => $"{claim.DashaLevel} {claim.DashaPlanet}".Trim()) ?? Array.Empty<string>();
            var windows = item.Answer?.TimingWindows?.Take(3).Select(window => $"{FormatDate(window.StartDate.DateTime)} - {FormatDate(window.EndDate.DateTime)} ({window.Strength})") ?? Array.Empty<string>();
            return $"<article class=\"insight-card\"><div class=\"insight-head\"><span>{Html(ValueOrDash(item.LifeArea))}</span><strong>{Html(tone)}</strong></div><p class=\"report-text\">{Html(string.Join(" · ", claims.Where(value => !string.IsNullOrWhiteSpace(value))))}</p><p class=\"report-text\">{Html(string.Join("; ", windows))}</p></article>";
        });
        AppendSection(html, "Life Area Readings", $"<div class=\"insight-grid\">{string.Join(string.Empty, cards)}</div>");
    }

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
    private static string BuildBirthProfileSection(JsonElement birthProfile, string name, string birthDate, string birthTime, string birthPlace)
    {
        var familyHtml = BuildFamilyFacts(birthProfile);
        return $"<div class=\"grid-3\">{Fact("Name", name)}{Fact("Date", birthDate)}{Fact("Time", birthTime)}</div><div style=\"height:7px\"></div><div class=\"grid-2\">{Fact("Place", birthPlace)}{Fact("Coordinates", BuildCoordinates(birthProfile))}</div>{familyHtml}";
    }

    private static string BuildFamilyFacts(JsonElement birthProfile)
    {
        var facts = new List<string>();
        void add(string label, params string[] names)
        {
            var value = GetString(birthProfile, names);
            if (!string.IsNullOrWhiteSpace(value))
            {
                facts.Add(Fact(label, value.Trim()));
            }
        }

        add("Surname", "surname", "Surname");
        add("Father", "fatherName", "FatherName");
        add("Mother", "motherName", "MotherName");
        add("Gotra", "gotra", "Gotra");
        if (facts.Count == 0)
        {
            return string.Empty;
        }

        var grid = facts.Count >= 3 ? "grid-3" : "grid-2";
        return $"<div style=\"height:7px\"></div><div class=\"{grid}\">{string.Join(string.Empty, facts)}</div>";
    }

    private static string BuildFamilyHeaderLine(JsonElement birthProfile)
    {
        var parts = new List<string>();
        void add(string label, params string[] names)
        {
            var value = GetString(birthProfile, names);
            if (!string.IsNullOrWhiteSpace(value))
            {
                parts.Add($"<span class=\"intro-muted\">{Html(label)}</span><strong>{Html(value.Trim())}</strong>");
            }
        }

        add("Father", "fatherName", "FatherName");
        add("Mother", "motherName", "MotherName");
        add("Gotra", "gotra", "Gotra");
        if (parts.Count == 0)
        {
            return string.Empty;
        }

        return $"<p class=\"person-detail-value family-detail-value\">{string.Join(" ", parts)}</p>";
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








