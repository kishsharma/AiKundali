using KundaliEngine.API.DTOs.Responses;
using KundaliEngine.Domain.Enums;
using KundaliEngine.Domain.Models;

namespace KundaliEngine.API.Services.YogaDoshaActivation
{
    public class YogaDoshaActivationService
    {
        public FormationActivationPeriodsDto CalculateActivationPeriods(
            string formationKey,
            string name,
            string type,
            IEnumerable<string> participatingPlanets,
            IEnumerable<MahadashaActivationPeriod> timeline,
            DateTime currentUtc)
        {
            var participants = participatingPlanets
                .Where(planet => !string.IsNullOrWhiteSpace(planet))
                .Select(planet => planet.Trim())
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            var grouped = new FormationActivationPeriodsDto();

            if (participants.Count == 0)
            {
                return grouped;
            }

            var windows = timeline
                .SelectMany(mahadasha => mahadasha.Antardashas.Select(antardasha =>
                    BuildWindow(formationKey, name, type, participants, mahadasha.Planet, antardasha, currentUtc)))
                .Where(window => window != null)
                .Select(window => window!)
                .OrderBy(window => window.StartDate)
                .ToList();

            grouped.CurrentlyActive = windows
                .Where(window => window.IsCurrentlyActive)
                .ToList();

            grouped.Upcoming = windows
                .Where(window => window.StartDate > currentUtc)
                .ToList();

            grouped.Past = windows
                .Where(window => window.EndDate <= currentUtc)
                .OrderByDescending(window => window.EndDate)
                .ToList();

            return grouped;
        }

        private static YogaDoshaActivationDto? BuildWindow(
            string formationKey,
            string name,
            string type,
            ISet<string> participants,
            Planet mahadashaPlanet,
            AntardashaPeriod antardasha,
            DateTime currentUtc)
        {
            string mahadashaName = mahadashaPlanet.ToString();
            string antardashaName = antardasha.SubPlanet.ToString();
            bool mahadashaParticipates = participants.Contains(mahadashaName);
            bool antardashaParticipates = participants.Contains(antardashaName);

            if (!mahadashaParticipates && !antardashaParticipates)
            {
                return null;
            }

            string activationLevel = mahadashaParticipates && antardashaParticipates
                ? "Strong"
                : mahadashaParticipates
                    ? "Active"
                    : "Mild";

            var triggeredBy = new List<string>();
            if (mahadashaParticipates)
            {
                triggeredBy.Add(mahadashaName);
            }

            if (antardashaParticipates && !triggeredBy.Contains(antardashaName, StringComparer.OrdinalIgnoreCase))
            {
                triggeredBy.Add(antardashaName);
            }

            return new YogaDoshaActivationDto
            {
                FormationKey = formationKey,
                Name = name,
                Type = type,
                StartDate = antardasha.StartDate,
                EndDate = antardasha.EndDate,
                MahadashaPlanet = mahadashaName,
                AntardashaPlanet = antardashaName,
                ActivationLevel = activationLevel,
                IsCurrentlyActive = currentUtc >= antardasha.StartDate && currentUtc < antardasha.EndDate,
                TriggeredBy = triggeredBy
            };
        }
    }

    public class MahadashaActivationPeriod
    {
        public Planet Planet { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public List<AntardashaPeriod> Antardashas { get; set; } = new();
    }
}
