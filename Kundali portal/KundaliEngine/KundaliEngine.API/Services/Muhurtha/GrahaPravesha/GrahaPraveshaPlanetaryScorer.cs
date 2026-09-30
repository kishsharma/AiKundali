using KundaliEngine.Domain.Enums;

namespace KundaliEngine.API.Services.Muhurtha.GrahaPravesha;

public static class GrahaPraveshaPlanetaryScorer
{
    private static readonly HashSet<int> FavourableChandraPositions = [1, 3, 6, 7, 10, 11];
    private static readonly HashSet<int> DifficultChandraPositions = [4, 8, 12];
    public static readonly int[] EventHouses = [4, 8, 11];

    public static (string Name, int ScoreImpact, bool Warning) EvaluateTara(Nakshatra natalNakshatra, Nakshatra eventNakshatra)
    {
        var inclusiveCount = (((int)eventNakshatra - (int)natalNakshatra + 27) % 27) + 1;
        var taraPosition = inclusiveCount % 9;
        if (taraPosition == 0)
        {
            taraPosition = 9;
        }

        return taraPosition switch
        {
            2 => ("Sampat", 8, false),
            3 => ("Vipat", -12, true),
            4 => ("Kshema", 8, false),
            5 => ("Pratyari", -12, true),
            6 => ("Sadhana", 8, false),
            7 => ("Naidhana", -15, true),
            8 => ("Mitra", 8, false),
            9 => ("Parama Mitra", 12, false),
            _ => ("Janma", -5, true)
        };
    }

    public static (int Position, int ScoreImpact, bool Warning) EvaluateChandra(ZodiacSign natalMoonSign, ZodiacSign eventMoonSign)
    {
        var position = (((int)eventMoonSign - (int)natalMoonSign + 12) % 12) + 1;
        if (FavourableChandraPositions.Contains(position))
        {
            return (position, 8, false);
        }

        if (DifficultChandraPositions.Contains(position))
        {
            return (position, -8, true);
        }

        return (position, 0, false);
    }

    public static int ScoreEventHouses(IEnumerable<(Planet Planet, int House)> planets)
    {
        var impact = 0;
        foreach (var planet in planets)
        {
            if (!EventHouses.Contains(planet.House))
            {
                continue;
            }

            if (planet.Planet is Planet.Moon or Planet.Venus or Planet.Jupiter)
            {
                impact += 4;
            }
            else if (planet.Planet is Planet.Sun or Planet.Mars or Planet.Saturn or Planet.Rahu or Planet.Ketu)
            {
                impact -= 3;
            }
        }

        return impact;
    }
}
