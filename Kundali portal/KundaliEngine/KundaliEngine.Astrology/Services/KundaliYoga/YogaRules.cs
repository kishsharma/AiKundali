using KundaliEngine.Domain.Enums;

namespace KundaliEngine.Astrology.Services.KundaliYoga
{
    public class GajakesariYogaRule : IYogaRule
    {
        public string Key => "gajakesari";

        public YogaRuleMatch? Evaluate(YogaEvaluationContext context)
        {
            if (!context.IsKendraFromMoon(Planet.Jupiter))
            {
                return null;
            }

            var moon = context.GetPlanet(Planet.Moon)!;
            var jupiter = context.GetPlanet(Planet.Jupiter)!;

            return context.CreateMatch(
                Key,
                new[] { Planet.Moon, Planet.Jupiter },
                new[] { moon.House, jupiter.House },
                $"Jupiter is in a Kendra from Moon: Moon is in house {(int)moon.House}, Jupiter is in house {(int)jupiter.House}.");
        }
    }

    public class BudhaAdityaYogaRule : IYogaRule
    {
        public string Key => "budha_aditya";

        public YogaRuleMatch? Evaluate(YogaEvaluationContext context)
        {
            if (!context.IsConjunction(Planet.Sun, Planet.Mercury))
            {
                return null;
            }

            var sun = context.GetPlanet(Planet.Sun)!;

            return context.CreateMatch(
                Key,
                new[] { Planet.Sun, Planet.Mercury },
                new[] { sun.House },
                $"Sun and Mercury are conjunct in house {(int)sun.House}.");
        }
    }

    public class ChandraMangalaYogaRule : IYogaRule
    {
        public string Key => "chandra_mangala";

        public YogaRuleMatch? Evaluate(YogaEvaluationContext context)
        {
            bool conjunct = context.IsConjunction(Planet.Moon, Planet.Mars);
            bool aspect = context.IsAspect(Planet.Moon, Planet.Mars) || context.IsAspect(Planet.Mars, Planet.Moon);

            if (!conjunct && !aspect)
            {
                return null;
            }

            var moon = context.GetPlanet(Planet.Moon)!;
            var mars = context.GetPlanet(Planet.Mars)!;
            string relation = conjunct ? "conjunct" : "mutually connected by graha drishti";

            return context.CreateMatch(
                Key,
                new[] { Planet.Moon, Planet.Mars },
                new[] { moon.House, mars.House },
                $"Moon and Mars are {relation}: Moon is in house {(int)moon.House}, Mars is in house {(int)mars.House}.");
        }
    }

    public class RuchakaYogaRule : PanchaMahapurushaYogaRule
    {
        public RuchakaYogaRule() : base("ruchaka", Planet.Mars)
        {
        }
    }

    public class BhadraYogaRule : PanchaMahapurushaYogaRule
    {
        public BhadraYogaRule() : base("bhadra", Planet.Mercury)
        {
        }
    }

    public class HamsaYogaRule : PanchaMahapurushaYogaRule
    {
        public HamsaYogaRule() : base("hamsa", Planet.Jupiter)
        {
        }
    }

    public class MalavyaYogaRule : PanchaMahapurushaYogaRule
    {
        public MalavyaYogaRule() : base("malavya", Planet.Venus)
        {
        }
    }

    public class SasaYogaRule : PanchaMahapurushaYogaRule
    {
        public SasaYogaRule() : base("sasa", Planet.Saturn)
        {
        }
    }

    public class ShakataYogaRule : IYogaRule
    {
        public string Key => "shakata";

        public YogaRuleMatch? Evaluate(YogaEvaluationContext context)
        {
            var moon = context.GetPlanet(Planet.Moon);
            var jupiter = context.GetPlanet(Planet.Jupiter);

            if (moon == null || jupiter == null)
            {
                return null;
            }

            int distance = context.DistanceBetweenHouses(moon.House, jupiter.House);

            if (distance is not (6 or 8))
            {
                return null;
            }

            return context.CreateMatch(
                Key,
                new[] { Planet.Moon, Planet.Jupiter },
                new[] { moon.House, jupiter.House },
                $"Jupiter is {distance}th from Moon: Moon is in house {(int)moon.House}, Jupiter is in house {(int)jupiter.House}.");
        }
    }

    public class ParivartanaYogaRule : IYogaRule
    {
        public string Key => "parivartana";

        public YogaRuleMatch? Evaluate(YogaEvaluationContext context)
        {
            var planets = context.GetAllPlanets()
                .Where(position => position.Planet is not Planet.Rahu and not Planet.Ketu)
                .ToList();

            for (int i = 0; i < planets.Count; i++)
            {
                for (int j = i + 1; j < planets.Count; j++)
                {
                    var first = planets[i];
                    var second = planets[j];

                    if (context.SignLordOf(first.Sign) == second.Planet
                        && context.SignLordOf(second.Sign) == first.Planet)
                    {
                        return context.CreateMatch(
                            Key,
                            new[] { first.Planet, second.Planet },
                            new[] { first.House, second.House },
                            $"{first.Planet} is in {first.Sign}, owned by {second.Planet}, and {second.Planet} is in {second.Sign}, owned by {first.Planet}.");
                    }
                }
            }

            return null;
        }
    }

    public class KendraTrikonaRajaYogaRule : IYogaRule
    {
        private static readonly HouseNumber[] KendraHouses =
        {
            HouseNumber.First,
            HouseNumber.Fourth,
            HouseNumber.Seventh,
            HouseNumber.Tenth
        };

        private static readonly HouseNumber[] TrikonaHouses =
        {
            HouseNumber.First,
            HouseNumber.Fifth,
            HouseNumber.Ninth
        };

        public string Key => "kendra_trikona_raja";

        public YogaRuleMatch? Evaluate(YogaEvaluationContext context)
        {
            var kendraLords = GetHouseLords(context, KendraHouses);
            var trikonaLords = GetHouseLords(context, TrikonaHouses);

            foreach (var kendraLord in kendraLords)
            {
                foreach (var trikonaLord in trikonaLords)
                {
                    if (kendraLord.Planet == trikonaLord.Planet)
                    {
                        continue;
                    }

                    var match = EvaluateAssociation(context, kendraLord, trikonaLord);

                    if (match != null)
                    {
                        return match;
                    }
                }
            }

            return null;
        }

        private YogaRuleMatch? EvaluateAssociation(
            YogaEvaluationContext context,
            HouseLordRole kendraLord,
            HouseLordRole trikonaLord)
        {
            var kendraPlanet = context.GetPlanet(kendraLord.Planet);
            var trikonaPlanet = context.GetPlanet(trikonaLord.Planet);

            if (kendraPlanet == null || trikonaPlanet == null)
            {
                return null;
            }

            string? association = null;

            if (context.IsConjunction(kendraLord.Planet, trikonaLord.Planet))
            {
                association = $"conjunction in house {(int)kendraPlanet.House}";
            }
            else if (context.IsAspect(kendraLord.Planet, trikonaLord.Planet)
                && context.IsAspect(trikonaLord.Planet, kendraLord.Planet))
            {
                association = "mutual graha drishti";
            }
            else if (IsExchange(context, kendraLord.Planet, trikonaLord.Planet))
            {
                association = $"exchange of signs ({kendraLord.Planet} in {kendraPlanet.Sign}, {trikonaLord.Planet} in {trikonaPlanet.Sign})";
            }

            if (association == null)
            {
                return null;
            }

            return context.CreateMatch(
                Key,
                new[] { kendraLord.Planet, trikonaLord.Planet },
                new[] { kendraLord.House, trikonaLord.House, kendraPlanet.House, trikonaPlanet.House },
                $"{kendraLord.Planet}, lord of Kendra house {(int)kendraLord.House}, associates with {trikonaLord.Planet}, lord of Trikona house {(int)trikonaLord.House}, through {association}.");
        }

        private static List<HouseLordRole> GetHouseLords(YogaEvaluationContext context, IEnumerable<HouseNumber> houses)
        {
            var roles = new List<HouseLordRole>();

            foreach (var house in houses)
            {
                var lord = context.HouseLord(house);

                if (lord.HasValue)
                {
                    roles.Add(new HouseLordRole(house, lord.Value));
                }
            }

            return roles.Distinct().ToList();
        }

        private static bool IsExchange(YogaEvaluationContext context, Planet first, Planet second)
        {
            var firstPosition = context.GetPlanet(first);
            var secondPosition = context.GetPlanet(second);

            return firstPosition != null
                && secondPosition != null
                && context.SignLordOf(firstPosition.Sign) == second
                && context.SignLordOf(secondPosition.Sign) == first;
        }

        private readonly record struct HouseLordRole(HouseNumber House, Planet Planet);
    }

    public class LakshmiYogaRule : IYogaRule
    {
        public string Key => "lakshmi";

        public YogaRuleMatch? Evaluate(YogaEvaluationContext context)
        {
            var ninthLord = context.HouseLord(HouseNumber.Ninth);

            if (ninthLord == null)
            {
                return null;
            }

            var ninthLordPosition = context.GetPlanet(ninthLord.Value);

            if (ninthLordPosition == null)
            {
                return null;
            }

            bool auspiciousHouse = context.IsKendra((int)ninthLordPosition.House)
                || context.IsTrikona((int)ninthLordPosition.House);
            bool strongByDignity = context.IsOwnSign(ninthLord.Value)
                || context.IsExalted(ninthLord.Value);

            if (!auspiciousHouse || !strongByDignity)
            {
                return null;
            }

            return context.CreateMatch(
                Key,
                new[] { ninthLord.Value },
                new[] { HouseNumber.Ninth, ninthLordPosition.House },
                $"The 9th lord {ninthLord.Value} is strong by dignity and placed in auspicious house {(int)ninthLordPosition.House}.");
        }
    }

    public class SaraswatiYogaRule : IYogaRule
    {
        private static readonly HashSet<int> QualifyingHouses = new() { 1, 2, 4, 5, 7, 9, 10 };

        public string Key => "saraswati";

        public YogaRuleMatch? Evaluate(YogaEvaluationContext context)
        {
            var requiredPlanets = new[] { Planet.Jupiter, Planet.Venus, Planet.Mercury };
            var positions = requiredPlanets
                .Select(context.GetPlanet)
                .Where(position => position != null)
                .ToList();

            if (positions.Count != requiredPlanets.Length
                || positions.Any(position => !QualifyingHouses.Contains((int)position!.House)))
            {
                return null;
            }

            return context.CreateMatch(
                Key,
                requiredPlanets,
                positions.Select(position => position!.House),
                "Jupiter, Venus, and Mercury are all placed in auspicious Saraswati Yoga houses.");
        }
    }

    public class AdhiYogaRule : IYogaRule
    {
        public string Key => "adhi";

        public YogaRuleMatch? Evaluate(YogaEvaluationContext context)
        {
            var moon = context.GetPlanet(Planet.Moon);

            if (moon == null)
            {
                return null;
            }

            var targetHouses = new[]
            {
                context.HouseFrom(moon.House, 6),
                context.HouseFrom(moon.House, 7),
                context.HouseFrom(moon.House, 8)
            };
            var benefics = targetHouses
                .SelectMany(context.GetPlanetsInHouse)
                .Where(position => context.IsNaturalBenefic(position.Planet))
                .ToList();

            if (benefics.Count == 0)
            {
                return null;
            }

            return context.CreateMatch(
                Key,
                benefics.Select(position => position.Planet).Append(Planet.Moon),
                benefics.Select(position => position.House).Append(moon.House),
                "Natural benefics occupy the 6th, 7th, or 8th houses from Moon.");
        }
    }

    public class DurdharaYogaRule : IYogaRule
    {
        public string Key => "durdhara";

        public YogaRuleMatch? Evaluate(YogaEvaluationContext context)
        {
            var moon = context.GetPlanet(Planet.Moon);

            if (moon == null)
            {
                return null;
            }

            var secondFromMoon = context.HouseFrom(moon.House, 2);
            var twelfthFromMoon = context.HouseFrom(moon.House, 12);
            var secondPlanets = context.GetPlanetsInHouse(secondFromMoon)
                .Where(position => context.IsQualifyingMoonSidePlanet(position.Planet))
                .ToList();
            var twelfthPlanets = context.GetPlanetsInHouse(twelfthFromMoon)
                .Where(position => context.IsQualifyingMoonSidePlanet(position.Planet))
                .ToList();

            if (secondPlanets.Count == 0 || twelfthPlanets.Count == 0)
            {
                return null;
            }

            var involved = secondPlanets
                .Concat(twelfthPlanets)
                .Select(position => position.Planet)
                .Append(Planet.Moon);

            return context.CreateMatch(
                Key,
                involved,
                new[] { moon.House, secondFromMoon, twelfthFromMoon },
                "Qualifying planets occupy both the 2nd and 12th houses from Moon.");
        }
    }

    public class AmalaYogaRule : IYogaRule
    {
        public string Key => "amala";

        public YogaRuleMatch? Evaluate(YogaEvaluationContext context)
        {
            var lagnaTenth = HouseNumber.Tenth;
            var moon = context.GetPlanet(Planet.Moon);
            var targetHouses = new List<HouseNumber> { lagnaTenth };

            if (moon != null)
            {
                targetHouses.Add(context.HouseFrom(moon.House, 10));
            }

            var benefics = targetHouses
                .Distinct()
                .SelectMany(context.GetPlanetsInHouse)
                .Where(position => context.IsNaturalBenefic(position.Planet))
                .ToList();

            if (benefics.Count == 0)
            {
                return null;
            }

            return context.CreateMatch(
                Key,
                benefics.Select(position => position.Planet),
                benefics.Select(position => position.House),
                "A natural benefic occupies the 10th house from Lagna or Moon.");
        }
    }

    public class ShubhaKartariYogaRule : KartariYogaRule
    {
        public ShubhaKartariYogaRule() : base("shubha_kartari", true)
        {
        }
    }

    public class PapaKartariYogaRule : KartariYogaRule
    {
        public PapaKartariYogaRule() : base("papa_kartari", false)
        {
        }
    }

    public abstract class PanchaMahapurushaYogaRule : IYogaRule
    {
        private readonly Planet _planet;

        protected PanchaMahapurushaYogaRule(string key, Planet planet)
        {
            Key = key;
            _planet = planet;
        }

        public string Key { get; }

        public YogaRuleMatch? Evaluate(YogaEvaluationContext context)
        {
            var position = context.GetPlanet(_planet);

            if (position == null
                || !context.IsKendra((int)position.House)
                || !context.IsPlanetStrong(_planet))
            {
                return null;
            }

            return context.CreateMatch(
                Key,
                new[] { _planet },
                new[] { position.House },
                $"{_planet} is strong by dignity and placed in Kendra house {(int)position.House}.");
        }
    }

    public abstract class KartariYogaRule : IYogaRule
    {
        private readonly bool _benefic;

        protected KartariYogaRule(string key, bool benefic)
        {
            Key = key;
            _benefic = benefic;
        }

        public string Key { get; }

        public YogaRuleMatch? Evaluate(YogaEvaluationContext context)
        {
            for (int house = 1; house <= 12; house++)
            {
                var target = (HouseNumber)house;
                var previousHouse = context.HouseFrom(target, 12);
                var nextHouse = context.HouseFrom(target, 2);
                var previousPlanets = context.GetPlanetsInHouse(previousHouse)
                    .Where(position => IsRequiredNature(context, position.Planet))
                    .ToList();
                var nextPlanets = context.GetPlanetsInHouse(nextHouse)
                    .Where(position => IsRequiredNature(context, position.Planet))
                    .ToList();

                if (previousPlanets.Count == 0 || nextPlanets.Count == 0)
                {
                    continue;
                }

                var involvedPlanets = previousPlanets
                    .Concat(nextPlanets)
                    .Select(position => position.Planet);
                string nature = _benefic ? "benefic" : "malefic/shadow";

                return context.CreateMatch(
                    Key,
                    involvedPlanets,
                    new[] { previousHouse, target, nextHouse },
                    $"House {house} is hemmed between {nature} planets in adjacent houses {(int)previousHouse} and {(int)nextHouse}.");
            }

            return null;
        }

        private bool IsRequiredNature(YogaEvaluationContext context, Planet planet)
        {
            return _benefic
                ? context.IsNaturalBenefic(planet)
                : context.IsNaturalMaleficOrShadow(planet);
        }
    }
}
