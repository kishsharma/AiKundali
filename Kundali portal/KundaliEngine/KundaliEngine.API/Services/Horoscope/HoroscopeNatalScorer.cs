using KundaliEngine.Astrology.Services.Astrology;
using KundaliEngine.Astrology.Services.KundaliDosha;
using KundaliEngine.Astrology.Services.KundaliYoga;
using KundaliEngine.Astrology.Services.Scoring;
using KundaliEngine.Domain.Enums;
using KundaliEngine.Domain.Models;
using KundaliEngine.KnowledgeBase.Services;

namespace KundaliEngine.API.Services.Horoscope;

public sealed class HoroscopeNatalScorer
{
    private readonly YogaDetectionService _yogaDetectionService;
    private readonly DoshaDetectionService _doshaDetectionService;
    private readonly YogaMetadataService _yogaMetadataService;
    private readonly DoshaMetadataService _doshaMetadataService;
    private readonly AspectService _aspectService;
    private readonly HouseScoreService _houseScoreService;

    public HoroscopeNatalScorer(
        YogaDetectionService yogaDetectionService,
        DoshaDetectionService doshaDetectionService,
        YogaMetadataService yogaMetadataService,
        DoshaMetadataService doshaMetadataService,
        AspectService aspectService,
        HouseScoreService houseScoreService)
    {
        _yogaDetectionService = yogaDetectionService;
        _doshaDetectionService = doshaDetectionService;
        _yogaMetadataService = yogaMetadataService;
        _doshaMetadataService = doshaMetadataService;
        _aspectService = aspectService;
        _houseScoreService = houseScoreService;
    }

    public HoroscopeNatalScore Score(
        IReadOnlyList<PlanetPosition> natalPlanets,
        ZodiacSign natalAscendant,
        Planet mahadasha,
        Planet antardasha)
    {
        var natalList = natalPlanets.ToList();
        var natalAspects = _aspectService.CalculateGrahaDrishti(natalList);
        var houseScores = _houseScoreService.CalculateAllHouseScores(natalAscendant, natalList, natalAspects);
        var houses = BuildHouses(natalAscendant, natalList, houseScores);

        var dashaLords = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            mahadasha.ToString(),
            antardasha.ToString()
        };

        var formations = new List<HoroscopeActiveFormation>();
        var support = 0;
        var pressure = 0;
        var facts = new List<string>();

        foreach (var yoga in _yogaDetectionService.DetectYogas(natalList, houses))
        {
            if (!yoga.InvolvedPlanets.Any(p => dashaLords.Contains(p)))
            {
                continue;
            }

            var meta = _yogaMetadataService.GetYoga(yoga.Key);
            var nature = meta?.Nature ?? string.Empty;
            var isChallenging = nature.Contains("malefic", StringComparison.OrdinalIgnoreCase)
                || nature.Contains("dosha", StringComparison.OrdinalIgnoreCase)
                || nature.Contains("inauspicious", StringComparison.OrdinalIgnoreCase);
            var tone = isChallenging ? "Challenging" : "Supportive";
            if (isChallenging)
            {
                pressure += 10;
            }
            else
            {
                support += 10;
            }

            var name = meta?.Name ?? yoga.Key;
            var fact = $"Natal yoga {name} is lit by running {mahadasha}/{antardasha}.";
            facts.Add(fact);
            formations.Add(new HoroscopeActiveFormation
            {
                Type = "Yoga",
                Key = yoga.Key,
                Name = name,
                Tone = tone,
                Fact = fact
            });
        }

        foreach (var dosha in _doshaDetectionService.DetectDoshas(natalList, houses).Where(d => d.IsPresent))
        {
            if (!dosha.InvolvedPlanets.Any(p => dashaLords.Contains(p)))
            {
                continue;
            }

            var meta = _doshaMetadataService.GetDosha(dosha.Key);
            var name = meta?.Name ?? dosha.Key;
            pressure += 12;
            var fact = $"Natal dosha {name} is lit by running {mahadasha}/{antardasha}.";
            facts.Add(fact);
            formations.Add(new HoroscopeActiveFormation
            {
                Type = "Dosha",
                Key = dosha.Key,
                Name = name,
                Tone = "Challenging",
                Fact = fact
            });
        }

        var lagnaScore = houseScores.FirstOrDefault(h => h.HouseNumber == HouseNumber.First)?.Score ?? 50;
        var moonHouse = natalList.FirstOrDefault(p => p.Planet == Planet.Moon)?.House ?? HouseNumber.First;
        var moonHouseScore = houseScores.FirstOrDefault(h => h.HouseNumber == moonHouse)?.Score ?? 50;
        support += (lagnaScore - 50) / 5;
        support += (moonHouseScore - 50) / 5;
        facts.Insert(0, $"Lagna house score {lagnaScore}; Moon-house score {moonHouseScore} (AV-style house strength).");

        var delta = support - pressure;
        var toneOverall = delta >= 8 ? "Supportive" : delta <= -8 ? "Challenging" : "Mixed";
        var score = Math.Clamp(50 + delta, 0, 100);

        return new HoroscopeNatalScore
        {
            Score = score,
            Tone = toneOverall,
            Facts = facts.Take(6).ToList(),
            ActiveFormations = formations.Take(8).ToList(),
            HouseScores = houseScores
        };
    }

    private static List<HouseDto> BuildHouses(
        ZodiacSign ascendant,
        IReadOnlyList<PlanetPosition> planets,
        IReadOnlyList<HouseScoreResult> houseScores)
    {
        var houses = new List<HouseDto>();
        for (var i = 1; i <= 12; i++)
        {
            var houseNumber = (HouseNumber)i;
            var signIndex = (((int)ascendant - 1) + (i - 1)) % 12;
            var sign = (ZodiacSign)(signIndex + 1);
            var score = houseScores.FirstOrDefault(h => h.HouseNumber == houseNumber);
            houses.Add(new HouseDto
            {
                HouseNumber = houseNumber,
                Sign = sign,
                Occupants = planets.Where(p => (int)p.House == i).Select(p => p.Planet).ToList(),
                Score = score?.Score ?? 50,
                Factors = score?.Factors ?? new List<string>(),
                DominantThemes = score?.DominantThemes ?? new List<string>()
            });
        }

        return houses;
    }
}

public sealed class HoroscopeNatalScore
{
    public int Score { get; set; }
    public string Tone { get; set; } = "Mixed";
    public List<string> Facts { get; set; } = new();
    public List<HoroscopeActiveFormation> ActiveFormations { get; set; } = new();
    public List<HouseScoreResult> HouseScores { get; set; } = new();
}

public sealed class HoroscopeActiveFormation
{
    public string Type { get; set; } = string.Empty;
    public string Key { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Tone { get; set; } = "Mixed";
    public string Fact { get; set; } = string.Empty;
}
