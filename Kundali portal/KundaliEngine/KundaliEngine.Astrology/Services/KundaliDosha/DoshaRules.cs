using KundaliEngine.Domain.Enums;
using KundaliEngine.Astrology.Services.KundaliYoga;

namespace KundaliEngine.Astrology.Services.KundaliDosha
{
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
            double start = Normalize(startLongitude);
            double end = Normalize(endLongitude);
            double target = Normalize(targetLongitude);

            if (start <= end)
            {
                return target >= start && target <= end;
            }

            return target >= start || target <= end;
        }

        private static double Normalize(double longitude)
        {
            double normalized = longitude % 360.0;
            return normalized < 0 ? normalized + 360.0 : normalized;
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
                    Separation = AngularSeparation(moon.Longitude, node!.Longitude),
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

        private static double AngularSeparation(double firstLongitude, double secondLongitude)
        {
            double difference = Math.Abs(Normalize(firstLongitude) - Normalize(secondLongitude));
            return difference > 180.0 ? 360.0 - difference : difference;
        }

        private static double Normalize(double longitude)
        {
            double normalized = longitude % 360.0;
            return normalized < 0 ? normalized + 360.0 : normalized;
        }
    }
}
