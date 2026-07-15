using KundaliEngine.Domain.Enums;
using KundaliEngine.Astrology.Services.KundaliYoga;

namespace KundaliEngine.Astrology.Services.KundaliDosha
{
    internal static class DoshaRuleMath
    {
        public static double AngularSeparation(double firstLongitude, double secondLongitude)
        {
            double difference = Math.Abs(Normalize(firstLongitude) - Normalize(secondLongitude));
            return difference > 180.0 ? 360.0 - difference : difference;
        }

        public static double Normalize(double longitude)
        {
            double normalized = longitude % 360.0;
            return normalized < 0 ? normalized + 360.0 : normalized;
        }
    }

    public class ManglikDoshaRule : IDoshaRule
    {
        private static readonly HashSet<int> SensitiveHouses = new() { 1, 4, 7, 8, 12 };

        public string Key => "manglik";

        public DoshaRuleMatch? Evaluate(YogaEvaluationContext context)
        {
            var mars = context.GetPlanet(Planet.Mars);

            if (mars == null)
            {
                return null;
            }

            bool fromLagna = SensitiveHouses.Contains((int)mars.House);
            bool fromMoon = false;
            int? moonRelativeHouse = null;
            var moon = context.GetPlanet(Planet.Moon);

            if (moon != null)
            {
                moonRelativeHouse = context.DistanceBetweenHouses(moon.House, mars.House);
                fromMoon = SensitiveHouses.Contains(moonRelativeHouse.Value);
            }

            if (!fromLagna && !fromMoon)
            {
                return null;
            }

            string severity = fromLagna && fromMoon ? "High" : fromLagna ? "Medium" : "Low";
            var reasons = new List<string>();

            if (fromLagna)
            {
                reasons.Add($"Mars is placed in house {(int)mars.House} from Lagna.");
            }

            if (fromMoon && moonRelativeHouse.HasValue)
            {
                reasons.Add($"Mars is {moonRelativeHouse.Value}th from Moon.");
            }

            return new DoshaRuleMatch
            {
                Key = Key,
                Severity = severity,
                InvolvedPlanets = new() { Planet.Mars.ToString() },
                InvolvedHouses = new[] { (int)mars.House }
                    .Concat(moonRelativeHouse.HasValue ? new[] { moonRelativeHouse.Value } : Array.Empty<int>())
                    .Distinct()
                    .OrderBy(house => house)
                    .ToList(),
                Reason = string.Join(" ", reasons)
            };
        }
    }

    public class KaalSarpaDoshaRule : IDoshaRule
    {
        public string Key => "kaal_sarpa";

        public DoshaRuleMatch? Evaluate(YogaEvaluationContext context)
        {
            var rahu = context.GetPlanet(Planet.Rahu);
            var ketu = context.GetPlanet(Planet.Ketu);

            if (rahu == null || ketu == null)
            {
                return null;
            }

            var planets = context.GetAllPlanets()
                .Where(position => position.Planet is not Planet.Rahu and not Planet.Ketu)
                .ToList();

            if (planets.Count == 0)
            {
                return null;
            }

            bool allBetweenRahuAndKetu = planets.All(position => IsBetweenClockwise(rahu.Longitude, ketu.Longitude, position.Longitude));
            bool allBetweenKetuAndRahu = planets.All(position => IsBetweenClockwise(ketu.Longitude, rahu.Longitude, position.Longitude));

            if (!allBetweenRahuAndKetu && !allBetweenKetuAndRahu)
            {
                return null;
            }

            string axis = allBetweenRahuAndKetu
                ? $"Rahu house {(int)rahu.House} to Ketu house {(int)ketu.House}"
                : $"Ketu house {(int)ketu.House} to Rahu house {(int)rahu.House}";

            return new DoshaRuleMatch
            {
                Key = Key,
                Severity = "High",
                InvolvedPlanets = new() { Planet.Rahu.ToString(), Planet.Ketu.ToString() },
                InvolvedHouses = new[] { (int)rahu.House, (int)ketu.House }.OrderBy(house => house).ToList(),
                Reason = $"All non-node planets fall within the {axis} arc."
            };
        }

        private static bool IsBetweenClockwise(double startLongitude, double endLongitude, double targetLongitude)
        {
            double start = DoshaRuleMath.Normalize(startLongitude);
            double end = DoshaRuleMath.Normalize(endLongitude);
            double target = DoshaRuleMath.Normalize(targetLongitude);

            if (start <= end)
            {
                return target >= start && target <= end;
            }

            return target >= start || target <= end;
        }
    }

    public class ChandraGrahanDoshaRule : IDoshaRule
    {
        private const double StrongOrbDegrees = 8.0;
        private const double ModerateOrbDegrees = 12.0;

        public string Key => "chandra_grahan";

        public DoshaRuleMatch? Evaluate(YogaEvaluationContext context)
        {
            var moon = context.GetPlanet(Planet.Moon);
            var rahu = context.GetPlanet(Planet.Rahu);
            var ketu = context.GetPlanet(Planet.Ketu);

            if (moon == null || (rahu == null && ketu == null))
            {
                return null;
            }

            var nodeMatches = new[] { rahu, ketu }
                .Where(node => node != null)
                .Select(node => new
                {
                    Node = node!,
                    Separation = DoshaRuleMath.AngularSeparation(moon.Longitude, node!.Longitude),
                    SameHouse = moon.House == node.House
                })
                .Where(match => match.SameHouse || match.Separation <= ModerateOrbDegrees)
                .OrderBy(match => match.Separation)
                .ToList();

            if (nodeMatches.Count == 0)
            {
                return null;
            }

            var closest = nodeMatches[0];
            string severity = closest.Separation <= StrongOrbDegrees
                ? "High"
                : closest.Separation <= ModerateOrbDegrees
                    ? "Medium"
                    : "Low";

            string nodeNames = string.Join(", ", nodeMatches.Select(match => match.Node.Planet.ToString()).Distinct());
            string reason = closest.Separation <= ModerateOrbDegrees
                ? $"Moon is close to {closest.Node.Planet} with {closest.Separation:0.##} degrees separation in house {(int)moon.House}."
                : $"Moon shares house {(int)moon.House} with {nodeNames}.";

            return new DoshaRuleMatch
            {
                Key = Key,
                Severity = severity,
                InvolvedPlanets = nodeMatches
                    .Select(match => match.Node.Planet.ToString())
                    .Append(Planet.Moon.ToString())
                    .Distinct()
                    .OrderBy(name => name)
                    .ToList(),
                InvolvedHouses = nodeMatches
                    .Select(match => (int)match.Node.House)
                    .Append((int)moon.House)
                    .Distinct()
                    .OrderBy(house => house)
                    .ToList(),
                Reason = reason
            };
        }
    }

    public class SuryaGrahanDoshaRule : IDoshaRule
    {
        private const double StrongOrbDegrees = 8.0;
        private const double ModerateOrbDegrees = 12.0;

        public string Key => "surya_grahan";

        public DoshaRuleMatch? Evaluate(YogaEvaluationContext context)
        {
            var sun = context.GetPlanet(Planet.Sun);
            var rahu = context.GetPlanet(Planet.Rahu);
            var ketu = context.GetPlanet(Planet.Ketu);

            if (sun == null || (rahu == null && ketu == null))
            {
                return null;
            }

            var nodeMatches = new[] { rahu, ketu }
                .Where(node => node != null)
                .Select(node => new
                {
                    Node = node!,
                    Separation = DoshaRuleMath.AngularSeparation(sun.Longitude, node!.Longitude),
                    SameHouse = sun.House == node.House
                })
                .Where(match => match.SameHouse || match.Separation <= ModerateOrbDegrees)
                .OrderBy(match => match.Separation)
                .ToList();

            if (nodeMatches.Count == 0)
            {
                return null;
            }

            var closest = nodeMatches[0];
            string severity = closest.Separation <= StrongOrbDegrees
                ? "High"
                : closest.Separation <= ModerateOrbDegrees
                    ? "Medium"
                    : "Low";

            string reason = closest.Separation <= ModerateOrbDegrees
                ? $"Sun is close to {closest.Node.Planet} with {closest.Separation:0.##} degrees separation in house {(int)sun.House}."
                : $"Sun shares house {(int)sun.House} with {string.Join(", ", nodeMatches.Select(match => match.Node.Planet.ToString()).Distinct())}.";

            return new DoshaRuleMatch
            {
                Key = Key,
                Severity = severity,
                InvolvedPlanets = nodeMatches
                    .Select(match => match.Node.Planet.ToString())
                    .Append(Planet.Sun.ToString())
                    .Distinct()
                    .OrderBy(name => name)
                    .ToList(),
                InvolvedHouses = nodeMatches
                    .Select(match => (int)match.Node.House)
                    .Append((int)sun.House)
                    .Distinct()
                    .OrderBy(house => house)
                    .ToList(),
                Reason = reason
            };
        }
    }

    public class GuruChandalDoshaRule : IDoshaRule
    {
        private const double StrongOrbDegrees = 8.0;
        private const double ModerateOrbDegrees = 12.0;

        public string Key => "guru_chandal";

        public DoshaRuleMatch? Evaluate(YogaEvaluationContext context)
        {
            var jupiter = context.GetPlanet(Planet.Jupiter);
            var rahu = context.GetPlanet(Planet.Rahu);
            var ketu = context.GetPlanet(Planet.Ketu);

            if (jupiter == null || (rahu == null && ketu == null))
            {
                return null;
            }

            var nodeMatches = new[] { rahu, ketu }
                .Where(node => node != null)
                .Select(node => new
                {
                    Node = node!,
                    Separation = DoshaRuleMath.AngularSeparation(jupiter.Longitude, node!.Longitude),
                    SameHouse = jupiter.House == node.House
                })
                .Where(match => match.SameHouse || match.Separation <= ModerateOrbDegrees)
                .OrderBy(match => match.Separation)
                .ToList();

            if (nodeMatches.Count == 0)
            {
                return null;
            }

            var closest = nodeMatches[0];
            string severity = closest.Separation <= StrongOrbDegrees
                ? "High"
                : closest.Separation <= ModerateOrbDegrees
                    ? "Medium"
                    : "Low";

            string reason = closest.Separation <= ModerateOrbDegrees
                ? $"Jupiter is close to {closest.Node.Planet} with {closest.Separation:0.##} degrees separation in house {(int)jupiter.House}."
                : $"Jupiter shares house {(int)jupiter.House} with {string.Join(", ", nodeMatches.Select(match => match.Node.Planet.ToString()).Distinct())}.";

            return new DoshaRuleMatch
            {
                Key = Key,
                Severity = severity,
                InvolvedPlanets = nodeMatches
                    .Select(match => match.Node.Planet.ToString())
                    .Append(Planet.Jupiter.ToString())
                    .Distinct()
                    .OrderBy(name => name)
                    .ToList(),
                InvolvedHouses = nodeMatches
                    .Select(match => (int)match.Node.House)
                    .Append((int)jupiter.House)
                    .Distinct()
                    .OrderBy(house => house)
                    .ToList(),
                Reason = reason
            };
        }
    }

    public class PitraDoshaRule : IDoshaRule
    {
        public string Key => "pitra";

        public DoshaRuleMatch? Evaluate(YogaEvaluationContext context)
        {
            var sun = context.GetPlanet(Planet.Sun);
            var ninthLord = context.HouseLord(HouseNumber.Ninth);

            var reasons = new List<string>();
            var involvedPlanets = new List<Planet>();
            var involvedHouses = new List<HouseNumber> { HouseNumber.Ninth };
            int score = 0;

            if (sun != null)
            {
                bool sunWithNode = context.IsConjunction(Planet.Sun, Planet.Rahu) || context.IsConjunction(Planet.Sun, Planet.Ketu);
                bool sunInNinthWithMalefic = sun.House == HouseNumber.Ninth
                    && (context.IsConjunction(Planet.Sun, Planet.Saturn)
                        || context.IsConjunction(Planet.Sun, Planet.Rahu)
                        || context.IsConjunction(Planet.Sun, Planet.Ketu));

                if (sunWithNode)
                {
                    score += 2;
                    reasons.Add("Sun is associated with Rahu or Ketu.");
                    involvedPlanets.Add(Planet.Sun);
                    involvedPlanets.Add(context.IsConjunction(Planet.Sun, Planet.Rahu) ? Planet.Rahu : Planet.Ketu);
                    involvedHouses.Add(sun.House);
                }

                if (sunInNinthWithMalefic)
                {
                    score += 2;
                    reasons.Add("Sun is afflicted in the 9th house of father, lineage, and blessings.");
                    involvedPlanets.Add(Planet.Sun);
                    involvedHouses.Add(HouseNumber.Ninth);
                }
            }

            if (ninthLord.HasValue)
            {
                var ninthLordPosition = context.GetPlanet(ninthLord.Value);
                if (ninthLordPosition != null)
                {
                    bool ninthLordAfflicted = context.IsDusthana((int)ninthLordPosition.House)
                        || context.IsConjunction(ninthLord.Value, Planet.Rahu)
                        || context.IsConjunction(ninthLord.Value, Planet.Ketu)
                        || context.IsConjunction(ninthLord.Value, Planet.Saturn);

                    if (ninthLordAfflicted)
                    {
                        score += 1;
                        reasons.Add($"The 9th lord {ninthLord.Value} is under dusthana, node, or Saturn pressure.");
                        involvedPlanets.Add(ninthLord.Value);
                        involvedHouses.Add(ninthLordPosition.House);
                    }
                }
            }

            if (score == 0)
            {
                return null;
            }

            return new DoshaRuleMatch
            {
                Key = Key,
                Severity = score >= 3 ? "High" : score == 2 ? "Medium" : "Low",
                InvolvedPlanets = involvedPlanets.Distinct().Select(planet => planet.ToString()).OrderBy(name => name).ToList(),
                InvolvedHouses = involvedHouses.Distinct().Select(house => (int)house).OrderBy(house => house).ToList(),
                Reason = string.Join(" ", reasons)
            };
        }
    }

    public class ShaniDoshaRule : IDoshaRule
    {
        private static readonly HashSet<int> SensitiveHouses = new() { 1, 4, 7, 8, 12 };

        public string Key => "shani";

        public DoshaRuleMatch? Evaluate(YogaEvaluationContext context)
        {
            var saturn = context.GetPlanet(Planet.Saturn);
            if (saturn == null)
            {
                return null;
            }

            bool fromLagna = SensitiveHouses.Contains((int)saturn.House);
            bool fromMoon = false;
            int? moonRelativeHouse = null;
            var moon = context.GetPlanet(Planet.Moon);

            if (moon != null)
            {
                moonRelativeHouse = context.DistanceBetweenHouses(moon.House, saturn.House);
                fromMoon = SensitiveHouses.Contains(moonRelativeHouse.Value);
            }

            bool saturnAfflictsMoon = context.IsAspect(Planet.Saturn, Planet.Moon);

            if (!fromLagna && !fromMoon && !saturnAfflictsMoon)
            {
                return null;
            }

            var reasons = new List<string>();
            if (fromLagna)
            {
                reasons.Add($"Saturn is placed in sensitive house {(int)saturn.House} from Lagna.");
            }

            if (fromMoon && moonRelativeHouse.HasValue)
            {
                reasons.Add($"Saturn is {moonRelativeHouse.Value}th from Moon.");
            }

            if (saturnAfflictsMoon)
            {
                reasons.Add("Saturn aspects Moon, adding emotional heaviness and delay pressure.");
            }

            string severity = (fromLagna && fromMoon) || saturnAfflictsMoon
                ? "Medium"
                : "Low";

            return new DoshaRuleMatch
            {
                Key = Key,
                Severity = severity,
                InvolvedPlanets = new() { Planet.Saturn.ToString() },
                InvolvedHouses = new[] { (int)saturn.House }
                    .Concat(moonRelativeHouse.HasValue ? new[] { moonRelativeHouse.Value } : Array.Empty<int>())
                    .Distinct()
                    .OrderBy(house => house)
                    .ToList(),
                Reason = string.Join(" ", reasons)
            };
        }
    }

    public class KemadrumaDoshaRule : IDoshaRule
    {
        public string Key => "kemadruma";

        public DoshaRuleMatch? Evaluate(YogaEvaluationContext context)
        {
            var moon = context.GetPlanet(Planet.Moon);
            if (moon == null)
            {
                return null;
            }

            var secondFromMoon = context.HouseFrom(moon.House, 2);
            var twelfthFromMoon = context.HouseFrom(moon.House, 12);

            bool hasPlanetSecond = context.GetPlanetsInHouse(secondFromMoon).Any(position => context.IsQualifyingMoonSidePlanet(position.Planet));
            bool hasPlanetTwelfth = context.GetPlanetsInHouse(twelfthFromMoon).Any(position => context.IsQualifyingMoonSidePlanet(position.Planet));

            if (hasPlanetSecond || hasPlanetTwelfth)
            {
                return null;
            }

            bool hasKendraSupport = context.GetAllPlanets()
                .Where(position => position.Planet != Planet.Moon)
                .Any(position => context.IsKendra(context.DistanceBetweenHouses(moon.House, position.House)));

            string severity = hasKendraSupport ? "Low" : "Medium";
            string reason = hasKendraSupport
                ? "Moon has no qualifying planets in the 2nd or 12th from it, though kendra support reduces the isolation."
                : "Moon has no qualifying planets in the 2nd, 12th, or kendras from it.";

            return new DoshaRuleMatch
            {
                Key = Key,
                Severity = severity,
                InvolvedPlanets = new() { Planet.Moon.ToString() },
                InvolvedHouses = new[] { (int)moon.House, (int)secondFromMoon, (int)twelfthFromMoon }
                    .Distinct()
                    .OrderBy(house => house)
                    .ToList(),
                Reason = reason
            };
        }
    }

    public class DaridraDoshaRule : IDoshaRule
    {
        public string Key => "daridra";

        public DoshaRuleMatch? Evaluate(YogaEvaluationContext context)
        {
            var secondLord = context.HouseLord(HouseNumber.Second);
            var eleventhLord = context.HouseLord(HouseNumber.Eleventh);

            var reasons = new List<string>();
            var involvedPlanets = new List<Planet>();
            var involvedHouses = new List<HouseNumber> { HouseNumber.Second, HouseNumber.Eleventh };

            EvaluateWealthLord(context, secondLord, "2nd lord", reasons, involvedPlanets, involvedHouses);
            EvaluateWealthLord(context, eleventhLord, "11th lord", reasons, involvedPlanets, involvedHouses);

            bool wealthHousesUnderNodePressure =
                context.GetPlanetsInHouse(HouseNumber.Second).Any(position => position.Planet is Planet.Rahu or Planet.Ketu)
                || context.GetPlanetsInHouse(HouseNumber.Eleventh).Any(position => position.Planet is Planet.Rahu or Planet.Ketu);

            if (wealthHousesUnderNodePressure)
            {
                reasons.Add("Rahu or Ketu occupies a wealth house, adding volatility to savings or gains.");
                involvedPlanets.AddRange(context.GetPlanetsInHouse(HouseNumber.Second)
                    .Concat(context.GetPlanetsInHouse(HouseNumber.Eleventh))
                    .Where(position => position.Planet is Planet.Rahu or Planet.Ketu)
                    .Select(position => position.Planet));
            }

            if (reasons.Count == 0)
            {
                return null;
            }

            return new DoshaRuleMatch
            {
                Key = Key,
                Severity = reasons.Count >= 3 ? "High" : reasons.Count == 2 ? "Medium" : "Low",
                InvolvedPlanets = involvedPlanets.Distinct().Select(planet => planet.ToString()).OrderBy(name => name).ToList(),
                InvolvedHouses = involvedHouses.Distinct().Select(house => (int)house).OrderBy(house => house).ToList(),
                Reason = string.Join(" ", reasons)
            };
        }

        private static void EvaluateWealthLord(
            YogaEvaluationContext context,
            Planet? lord,
            string label,
            List<string> reasons,
            List<Planet> involvedPlanets,
            List<HouseNumber> involvedHouses)
        {
            if (!lord.HasValue)
            {
                return;
            }

            var position = context.GetPlanet(lord.Value);
            if (position == null)
            {
                return;
            }

            bool inDusthana = context.IsDusthana((int)position.House);
            bool afflictedByNode = context.IsConjunction(lord.Value, Planet.Rahu) || context.IsConjunction(lord.Value, Planet.Ketu);
            bool afflictedBySaturn = context.IsConjunction(lord.Value, Planet.Saturn) || context.IsAspect(Planet.Saturn, lord.Value);

            if (!inDusthana && !afflictedByNode && !afflictedBySaturn)
            {
                return;
            }

            var conditions = new List<string>();
            if (inDusthana)
            {
                conditions.Add($"placed in dusthana house {(int)position.House}");
            }

            if (afflictedByNode)
            {
                conditions.Add("associated with Rahu or Ketu");
            }

            if (afflictedBySaturn)
            {
                conditions.Add("under Saturn pressure");
            }

            reasons.Add($"{label} {lord.Value} is {string.Join(", ", conditions)}.");
            involvedPlanets.Add(lord.Value);
            involvedHouses.Add(position.House);
        }
    }
}
