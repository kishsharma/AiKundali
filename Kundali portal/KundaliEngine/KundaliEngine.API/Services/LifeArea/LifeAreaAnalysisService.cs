using System.Text.RegularExpressions;
using KundaliEngine.API.DTOs.LifeArea;
using KundaliEngine.Astrology.Services.DivisionalCharts;
using KundaliEngine.Astrology.Services.Scoring;
using KundaliEngine.Domain.Enums;
using KundaliEngine.Domain.Models;
using KundaliEngine.Domain.Models.DivisionalCharts;
using KundaliEngine.KnowledgeBase.Models;
using KundaliEngine.KnowledgeBase.Services;

namespace KundaliEngine.API.Services.LifeArea;

public sealed class LifeAreaAnalysisService
{
    private static readonly Regex LordPattern = new(@"^(?<house>\d+)(?:st|nd|rd|th)Lord$", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Planet[] KarakaPlanets = [Planet.Sun, Planet.Moon, Planet.Mars, Planet.Mercury, Planet.Jupiter, Planet.Venus, Planet.Saturn];

    private readonly DivisionalChartMetadataService _metadata;
    private readonly AstrologyKnowledgeService _knowledge;
    private readonly DivisionalChartService _divisionalCharts;
    private readonly PlanetScoreService _scores;

    public LifeAreaAnalysisService(
        DivisionalChartMetadataService metadata,
        AstrologyKnowledgeService knowledge,
        DivisionalChartService divisionalCharts)
    {
        _metadata = metadata;
        _knowledge = knowledge;
        _divisionalCharts = divisionalCharts;
        _scores = new PlanetScoreService(knowledge);
    }

    public IReadOnlyList<LifeAreaCatalogItemDto> GetCatalog(string? locale)
    {
        var language = LifeAreaCopy.Locale(locale);
        return _metadata.GetAreas()
            .Where(pair => !pair.Key.Equals("vehicles", StringComparison.OrdinalIgnoreCase))
            .Select(pair => new LifeAreaCatalogItemDto
            {
                Key = pair.Key,
                Name = LifeAreaCopy.AreaName(pair.Key, language),
                PrimaryCharts = pair.Value.PrimaryCharts,
                SupportingCharts = pair.Value.SupportingCharts,
                RequiredHouses = pair.Value.RequiredHouses,
                RequiredPlanets = pair.Value.RequiredPlanets
            })
            .ToList();
    }

    public LifeAreaReportDto Analyze(
        KundaliChart chart,
        double ascendantDegreeInSign,
        string areaKey,
        string? locale,
        LifeAreaTiming? timing)
    {
        if (!_metadata.TryGetArea(areaKey, out var area))
        {
            throw new ArgumentException($"Unknown life area '{areaKey}'.");
        }

        var language = LifeAreaCopy.Locale(locale);
        var canonicalKey = _metadata.GetAreas().First(pair => pair.Key.Equals(areaKey, StringComparison.OrdinalIgnoreCase)).Key;
        var areaName = LifeAreaCopy.AreaName(canonicalKey, language);
        var consulted = area.PrimaryCharts.Concat(area.SupportingCharts).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        var specialistCode = area.PrimaryCharts.FirstOrDefault(code => !code.Equals("D1", StringComparison.OrdinalIgnoreCase))
            ?? area.SupportingCharts.FirstOrDefault()
            ?? "D1";

        var natal = chart;
        var charts = new Dictionary<string, DivisionalChart>(StringComparer.OrdinalIgnoreCase);
        foreach (var code in consulted)
        {
            if (!Enum.TryParse<DivisionalChartType>(code, true, out var chartType))
            {
                continue;
            }

            charts[code] = _divisionalCharts.GenerateDivisionalChart(natal, chartType, ascendantDegreeInSign);
        }

        charts.TryGetValue(specialistCode, out var specialist);
        var subjects = ResolveSubjects(area.RequiredPlanets, natal);
        var factors = new List<LifeAreaFactorDto>();
        var planetTones = new Dictionary<Planet, string>();
        var agree = 0;
        var differ = 0;

        foreach (var subject in subjects)
        {
            var natalPlanet = natal.PlanetPositions.FirstOrDefault(item => item.Planet == subject.Planet);
            if (natalPlanet is null)
            {
                continue;
            }

            var specialistPlanet = specialist?.PlanetPositions.FirstOrDefault(item => item.Planet == subject.Planet) ?? natalPlanet;
            var natalDignity = DignityKey(natalPlanet);
            var specialistDignity = DignityKey(specialistPlanet);
            var natalTone = Tone(Score(natalPlanet));
            var specialistTone = Tone(Score(specialistPlanet));
            var factorTone = Combine(natalTone, specialistTone);
            if (specialistCode == "D1" || natalTone == specialistTone)
            {
                agree++;
            }
            else
            {
                differ++;
            }

            var planetLabel = LifeAreaCopy.PlanetName(subject.Planet, language);
            factors.Add(new LifeAreaFactorDto
            {
                Subject = string.IsNullOrWhiteSpace(subject.Role) ? planetLabel : $"{planetLabel} ({subject.Role})",
                NatalSign = LifeAreaCopy.SignName(natalPlanet.Sign, language),
                NatalDignity = LifeAreaCopy.Dignity(natalDignity, language),
                SpecialistChart = specialistCode,
                SpecialistSign = LifeAreaCopy.SignName(specialistPlanet.Sign, language),
                SpecialistDignity = LifeAreaCopy.Dignity(specialistDignity, language),
                Tone = factorTone,
                Reading = LifeAreaCopy.FactorReading(
                    planetLabel,
                    LifeAreaCopy.SignName(natalPlanet.Sign, language),
                    LifeAreaCopy.Dignity(natalDignity, language),
                    specialistCode,
                    LifeAreaCopy.SignName(specialistPlanet.Sign, language),
                    LifeAreaCopy.Dignity(specialistDignity, language),
                    natalPlanet.IsRetrograde,
                    language)
            });
            planetTones[subject.Planet] = factorTone;
        }

        var support = factors.Count(item => item.Tone == "support");
        var pressure = factors.Count(item => item.Tone == "pressure");
        var verdict = support > pressure && support > 0
            ? "supported"
            : pressure > support
                ? "needsCare"
                : "mixed";
        var connected = timing is not null && subjects.Any(subject =>
            subject.Planet.ToString().Equals(timing.Mahadasha, StringComparison.OrdinalIgnoreCase)
            || subject.Planet.ToString().Equals(timing.Antardasha, StringComparison.OrdinalIgnoreCase));

        return new LifeAreaReportDto
        {
            AreaKey = canonicalKey,
            AreaName = areaName,
            Verdict = verdict,
            VerdictText = LifeAreaCopy.VerdictText(verdict, areaName, language),
            ChartsConsulted = consulted,
            SpecialistChart = specialistCode,
            Factors = factors,
            CrossValidation = LifeAreaCopy.CrossValidation(agree, differ, specialistCode, language),
            Timing = LifeAreaCopy.Timing(
                LocalPlanet(timing?.Mahadasha, language),
                LocalPlanet(timing?.Antardasha, language),
                connected,
                language),
            Periods = BuildPeriods(timing, planetTones, language),
            Note = LifeAreaCopy.Note(area.Note, consulted.Any(code => code.Equals("D60", StringComparison.OrdinalIgnoreCase)), language),
            PlainMeaning = LifeAreaCopy.PlainMeaning(
                canonicalKey,
                verdict,
                specialistCode,
                agree,
                differ,
                !string.IsNullOrWhiteSpace(timing?.Mahadasha) && !string.IsNullOrWhiteSpace(timing?.Antardasha),
                connected,
                language)
        };
    }

    private static List<LifeAreaPeriodDto> BuildPeriods(
        LifeAreaTiming? timing,
        IReadOnlyDictionary<Planet, string> planetTones,
        string language)
    {
        if (timing?.Windows is null || timing.Windows.Count == 0)
        {
            return [];
        }

        var periods = new List<LifeAreaPeriodDto>();
        foreach (var window in timing.Windows)
        {
            if (!Enum.TryParse<Planet>(window.Planet, true, out var planet))
            {
                continue;
            }

            planetTones.TryGetValue(planet, out var factorTone);
            var tone = factorTone == "support" ? "support" : "care";
            var name = LifeAreaCopy.PlanetName(planet, language);
            var label = LifeAreaCopy.MonthSpan(window.Start, window.End, language);
            periods.Add(new LifeAreaPeriodDto
            {
                Label = label,
                Planet = name,
                Tone = tone,
                Reading = LifeAreaCopy.PeriodReading(name, label, tone, language)
            });
        }

        return periods;
    }

    private List<LifeSubject> ResolveSubjects(IReadOnlyList<string> tokens, KundaliChart chart)
    {
        var karakas = BuildKarakas(chart.PlanetPositions);
        var subjects = new List<LifeSubject>();
        var seen = new HashSet<Planet>();

        foreach (var token in tokens)
        {
            Planet? planet = null;
            string? role = null;
            if (token.Equals("Darakaraka", StringComparison.OrdinalIgnoreCase) && karakas.TryGetValue("Darakaraka", out var dara))
            {
                planet = dara;
                role = "Darakaraka";
            }
            else if (token.Equals("Putrakaraka", StringComparison.OrdinalIgnoreCase) && karakas.TryGetValue("Putrakaraka", out var putra))
            {
                planet = putra;
                role = "Putrakaraka";
            }
            else if (LordHouse(token) is int house)
            {
                planet = SignLord(HouseSign(chart.AscendantSign, house));
                role = token;
            }
            else if (Enum.TryParse<Planet>(token, true, out var named))
            {
                planet = named;
            }

            if (planet is null || !seen.Add(planet.Value))
            {
                continue;
            }

            subjects.Add(new LifeSubject(planet.Value, role));
        }

        return subjects;
    }

    private static Dictionary<string, Planet> BuildKarakas(IEnumerable<PlanetPosition> planets)
    {
        var ordered = planets
            .Where(item => KarakaPlanets.Contains(item.Planet))
            .OrderByDescending(item => item.DegreeInSign)
            .ThenBy(item => item.Planet)
            .ToList();
        var names = new[] { "Atmakaraka", "Amatyakaraka", "Bhratrukaraka", "Matrukaraka", "Putrakaraka", "Gnatikaraka", "Darakaraka" };
        var result = new Dictionary<string, Planet>(StringComparer.OrdinalIgnoreCase);
        for (var index = 0; index < ordered.Count && index < names.Length; index++)
        {
            result[names[index]] = ordered[index].Planet;
        }

        return result;
    }

    private int Score(PlanetPosition position)
    {
        var score = _scores.Calculate(position).Score;
        return position.IsRetrograde ? Math.Max(0, score - 8) : score;
    }

    private static string Tone(int score) => score >= 60 ? "support" : score <= 42 ? "pressure" : "mixed";

    private static string Combine(string natal, string specialist)
    {
        if (natal == "support" && specialist == "support")
        {
            return "support";
        }

        if (natal == "pressure" && specialist == "pressure")
        {
            return "pressure";
        }

        return "mixed";
    }

    private string DignityKey(PlanetPosition position)
    {
        var knowledge = _knowledge.GetPlanet(position.Planet.ToString());
        if (knowledge is null)
        {
            return "neutral";
        }

        var signName = position.Sign.ToString();
        if (Equals(knowledge.Exaltation, signName))
        {
            return "exalted";
        }

        if (Equals(knowledge.Debilitation, signName))
        {
            return "debilitated";
        }

        if (Equals(knowledge.Moolatrikona, signName))
        {
            return "moolatrikona";
        }

        if (knowledge.OwnSigns.Any(sign => Equals(sign, signName)))
        {
            return "own";
        }

        var lord = SignLord(position.Sign).ToString();
        if (knowledge.Friends.Any(friend => Equals(friend, lord)))
        {
            return "friend";
        }

        if (knowledge.Enemies.Any(enemy => Equals(enemy, lord)))
        {
            return "enemy";
        }

        return "neutral";
    }

    private static Planet SignLord(ZodiacSign sign) => sign switch
    {
        ZodiacSign.Aries or ZodiacSign.Scorpio => Planet.Mars,
        ZodiacSign.Taurus or ZodiacSign.Libra => Planet.Venus,
        ZodiacSign.Gemini or ZodiacSign.Virgo => Planet.Mercury,
        ZodiacSign.Cancer => Planet.Moon,
        ZodiacSign.Leo => Planet.Sun,
        ZodiacSign.Sagittarius or ZodiacSign.Pisces => Planet.Jupiter,
        _ => Planet.Saturn
    };

    private static ZodiacSign HouseSign(ZodiacSign lagna, int house)
    {
        var index = ((int)lagna - 1 + house - 1) % 12;
        return (ZodiacSign)(index + 1);
    }

    private static int? LordHouse(string token)
    {
        if (token.Equals("LagnaLord", StringComparison.OrdinalIgnoreCase))
        {
            return 1;
        }

        var match = LordPattern.Match(token);
        if (match.Success && int.TryParse(match.Groups["house"].Value, out var house) && house is >= 1 and <= 12)
        {
            return house;
        }

        return null;
    }

    private static string? LocalPlanet(string? name, string locale)
    {
        return Enum.TryParse<Planet>(name, true, out var planet)
            ? LifeAreaCopy.PlanetName(planet, locale)
            : name;
    }

    private static bool Equals(string? left, string right) =>
        string.Equals(left, right, StringComparison.OrdinalIgnoreCase);

    private readonly record struct LifeSubject(Planet Planet, string? Role);
}
