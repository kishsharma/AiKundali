using KundaliEngine.Astrology.Services.Astrology;
using KundaliEngine.Domain.Enums;
using KundaliEngine.Domain.Models;

namespace KundaliEngine.API.Services.Horoscope;

public static class HoroscopeAspectScorer
{
    private static readonly HashSet<Planet> Benefics =
    [
        Planet.Jupiter, Planet.Venus, Planet.Mercury, Planet.Moon
    ];

    private static readonly HashSet<Planet> Malefics =
    [
        Planet.Saturn, Planet.Mars, Planet.Rahu, Planet.Ketu, Planet.Sun
    ];

    public static void AppendAspectHits(
        HoroscopeGocharaScore score,
        AspectService aspectService,
        IReadOnlyList<PlanetPosition> transitFromLagna,
        IReadOnlyList<PlanetPosition> transitFromMoon,
        Planet mahadasha,
        Planet antardasha)
    {
        AppendForReference(score, aspectService, transitFromLagna.ToList(), "Lagna", mahadasha, antardasha, HouseNumber.First);
        AppendForReference(score, aspectService, transitFromMoon.ToList(), "Moon", mahadasha, antardasha, HouseNumber.First);
    }

    private static void AppendForReference(
        HoroscopeGocharaScore score,
        AspectService aspectService,
        List<PlanetPosition> planets,
        string reference,
        Planet mahadasha,
        Planet antardasha,
        HouseNumber lagnaHouse)
    {
        var aspects = aspectService.CalculateGrahaDrishti(planets);
        var moon = planets.FirstOrDefault(p => p.Planet == Planet.Moon);
        var md = planets.FirstOrDefault(p => p.Planet == mahadasha);
        var ad = planets.FirstOrDefault(p => p.Planet == antardasha);

        foreach (var houseAspect in aspects.HouseAspects.Where(a => a.ToHouse == (int)lagnaHouse))
        {
            AddHit(score, houseAspect.FromPlanet, reference, (int)lagnaHouse, planets, $"aspects {reference} lagna");
        }

        if (moon != null)
        {
            foreach (var planetAspect in aspects.PlanetAspects.Where(a => a.ToPlanet == Planet.Moon))
            {
                AddHit(score, planetAspect.FromPlanet, reference, (int)moon.House, planets, $"aspects Moon from {reference}");
            }
        }

        if (md != null)
        {
            foreach (var planetAspect in aspects.PlanetAspects.Where(a => a.ToPlanet == mahadasha))
            {
                AddHit(score, planetAspect.FromPlanet, reference, (int)md.House, planets, $"aspects Mahadasha lord {mahadasha}");
            }
        }

        if (ad != null && ad.Planet != mahadasha)
        {
            foreach (var planetAspect in aspects.PlanetAspects.Where(a => a.ToPlanet == antardasha))
            {
                AddHit(score, planetAspect.FromPlanet, reference, (int)ad.House, planets, $"aspects Antardasha lord {antardasha}");
            }
        }
    }

    private static void AddHit(
        HoroscopeGocharaScore score,
        Planet fromPlanet,
        string reference,
        int house,
        IReadOnlyList<PlanetPosition> planets,
        string clause)
    {
        var sign = planets.FirstOrDefault(p => p.Planet == fromPlanet)?.Sign.ToString() ?? string.Empty;
        var tone = Benefics.Contains(fromPlanet)
            ? "Supportive"
            : Malefics.Contains(fromPlanet)
                ? "Challenging"
                : "Mixed";

        if (tone == "Supportive")
        {
            score.Support += 6;
        }
        else if (tone == "Challenging")
        {
            score.Pressure += 6;
        }

        var fact = $"{fromPlanet} {clause} (house {house} from {reference}).";
        score.Hits.Add(new HoroscopeGocharaHit
        {
            Planet = fromPlanet.ToString(),
            Reference = $"{reference}-aspect",
            House = house,
            Sign = sign,
            Tone = tone,
            Fact = fact
        });
        if (tone != "Mixed")
        {
            score.Facts.Add(fact);
        }
    }

    public static void RecalculateOverall(HoroscopeGocharaScore score)
    {
        var delta = score.Support - score.Pressure;
        score.Tone = delta >= 12 ? "Supportive" : delta <= -12 ? "Challenging" : "Mixed";
        score.Score = Math.Clamp(50 + delta, 0, 100);
        score.Hits = score.Hits
            .GroupBy(h => $"{h.Planet}|{h.Reference}|{h.House}|{h.Fact}")
            .Select(g => g.First())
            .OrderByDescending(h => h.Tone == "Challenging" ? 3 : h.Tone == "Supportive" ? 2 : 1)
            .Take(18)
            .ToList();
        score.Facts = score.Hits.Where(h => h.Tone != "Mixed").Select(h => h.Fact).Take(10).ToList();
    }
}
