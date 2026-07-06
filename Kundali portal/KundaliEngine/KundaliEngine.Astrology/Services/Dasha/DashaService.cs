using KundaliEngine.Domain.Enums;
using KundaliEngine.Domain.Models;
using KundaliEngine.Domain.Rules;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KundaliEngine.Astrology.Services.Dasha
{
    public class DashaService
    {
        private const double VimshottariTotalYears = 120.0;
        private const double DaysPerYear = 365.25;

        public (Planet Planet, double RemainingYears) GetBirthMahadasha(PlanetPosition moon)
        {
            int nakIndex = (int)(moon.Longitude / (360.0 / 27));
            Planet dashaLord = NakshatraDashaMapper.GetDashaLord(nakIndex);

            var dasha = DashaRules.VimshottariOrder
                .First(d => d.Planet == dashaLord);

            double nakStart = nakIndex * (360.0 / 27);
            double progress = (moon.Longitude - nakStart) / (360.0 / 27);

            double remaining = dasha.Years * (1 - progress);

            return (dashaLord, remaining);
        }

        public List<MahadashaPeriod> GenerateMahadashaTimeline(Planet startPlanet, double remainingYears, DateTime birthDate)
        {
            var result = new List<MahadashaPeriod>();

            var order = DashaRules.VimshottariOrder;

            int startIndex = order.FindIndex(d => d.Planet == startPlanet);

            DateTime currentStart = birthDate;

            var first = order[startIndex];
            DateTime firstEnd = AddDashaYears(currentStart, remainingYears);

            result.Add(new MahadashaPeriod
            {
                Planet = first.Planet,
                StartDate = currentStart,
                EndDate = firstEnd
            });

            currentStart = firstEnd;

            // 🔥 NEXT FULL CYCLE
            int index = (startIndex + 1) % order.Count;

            for (int i = 0; i < order.Count - 1; i++)
            {
                var dasha = order[index];

                DateTime end = AddDashaYears(currentStart, dasha.Years);

                result.Add(new MahadashaPeriod
                {
                    Planet = dasha.Planet,
                    StartDate = currentStart,
                    EndDate = end
                });

                currentStart = end;

                index = (index + 1) % order.Count;
            }

            return result;
        }

        public List<MahadashaPeriod> GenerateMahadashaTimelineForYears(
            Planet startPlanet,
            double remainingYears,
            DateTime birthDate,
            double yearsToCover)
        {
            var result = new List<MahadashaPeriod>();
            var order = DashaRules.VimshottariOrder;
            int startIndex = order.FindIndex(d => d.Planet == startPlanet);
            DateTime timelineEnd = AddDashaYears(birthDate, yearsToCover);

            DateTime currentStart = birthDate;
            int index = startIndex;
            double firstDurationYears = remainingYears;
            bool firstPeriod = true;

            while (currentStart < timelineEnd)
            {
                var dasha = order[index];
                double durationYears = firstPeriod ? firstDurationYears : dasha.Years;
                DateTime end = AddDashaYears(currentStart, durationYears);

                result.Add(new MahadashaPeriod
                {
                    Planet = dasha.Planet,
                    StartDate = currentStart,
                    EndDate = end
                });

                currentStart = end;
                index = (index + 1) % order.Count;
                firstPeriod = false;
            }

            return result;
        }


        public List<AntardashaPeriod> GenerateAntardasha(MahadashaPeriod maha, double actualMahadashaYears)
        {
            return GenerateAntardasha(maha, actualMahadashaYears, maha.StartDate, maha.EndDate);
        }

        public List<AntardashaPeriod> GenerateAntardashaForTimelinePeriod(
            MahadashaPeriod maha,
            Planet birthMahadashaPlanet,
            double birthMahadashaRemainingYears)
        {
            double currentMahadashaYears = GetMahadashaYearsForTimelinePeriod(
                maha,
                birthMahadashaPlanet,
                birthMahadashaRemainingYears);

            return GenerateAntardashaForTimelinePeriod(
                maha,
                birthMahadashaPlanet,
                birthMahadashaRemainingYears,
                currentMahadashaYears);
        }

        public DashaCalculationResult CalculateCurrentDasha(
            Planet birthMahadashaPlanet,
            double birthMahadashaRemainingYears,
            DateTime birthDate,
            DateTime currentDateUtc)
        {
            var mahadashaTimeline = GenerateMahadashaTimeline(
                birthMahadashaPlanet,
                birthMahadashaRemainingYears,
                birthDate);

            var currentMaha = mahadashaTimeline
                .FirstOrDefault(m => currentDateUtc >= m.StartDate && currentDateUtc < m.EndDate)
                ?? mahadashaTimeline.First();

            double currentMahadashaYears = GetMahadashaYearsForTimelinePeriod(
                currentMaha,
                birthMahadashaPlanet,
                birthMahadashaRemainingYears);

            var currentAntarList = GenerateAntardashaForTimelinePeriod(
                currentMaha,
                birthMahadashaPlanet,
                birthMahadashaRemainingYears,
                currentMahadashaYears);

            var currentAntar = currentAntarList
                .FirstOrDefault(a => currentDateUtc >= a.StartDate && currentDateUtc < a.EndDate)
                ?? currentAntarList.First();

            return new DashaCalculationResult
            {
                MahadashaTimeline = mahadashaTimeline,
                CurrentMahadasha = currentMaha,
                CurrentAntardashaTimeline = currentAntarList,
                CurrentAntardasha = currentAntar
            };
        }

        private List<AntardashaPeriod> GenerateAntardasha(
            MahadashaPeriod maha,
            double actualMahadashaYears,
            DateTime calculationStart,
            DateTime calculationEnd)
        {
            var result = new List<AntardashaPeriod>();

            var order = DashaRules.VimshottariOrder;

            int startIndex = order.FindIndex(d => d.Planet == maha.Planet);

            DateTime currentStart = maha.StartDate;

            for (int i = 0; i < order.Count; i++)
            {
                var sub = order[(startIndex + i) % order.Count];

                double durationYears = actualMahadashaYears * sub.Years / VimshottariTotalYears;
                DateTime end = AddDashaYears(currentStart, durationYears);

                result.Add(new AntardashaPeriod
                {
                    MainPlanet = maha.Planet,
                    SubPlanet = sub.Planet,
                    StartDate = currentStart,
                    EndDate = end
                });

                currentStart = end;
            }

            return ClipAntardashas(result, calculationStart, calculationEnd);
        }

        public (Planet Maha, Planet Antar) GetCurrentDasha(List<MahadashaPeriod> mahaTimeline, List<AntardashaPeriod> antarTimeline, DateTime currentDate)
        {
            var currentMaha = mahaTimeline.FirstOrDefault(d => currentDate >= d.StartDate && currentDate < d.EndDate);

            var currentAntar = antarTimeline.FirstOrDefault(d => currentDate >= d.StartDate && currentDate < d.EndDate);

            if (currentMaha == null)
            {
                throw new InvalidOperationException($"No Mahadasha period covers {currentDate:yyyy-MM-dd}.");
            }

            if (currentAntar == null)
            {
                throw new InvalidOperationException($"No Antardasha period covers {currentDate:yyyy-MM-dd}.");
            }

            return (currentMaha.Planet, currentAntar.SubPlanet);
        }

        private int GetYears(Planet planet)
        {
            return DashaRules.VimshottariOrder
                .First(d => d.Planet == planet).Years;
        }

        private List<AntardashaPeriod> GenerateAntardashaForTimelinePeriod(
            MahadashaPeriod maha,
            Planet birthMahadashaPlanet,
            double birthMahadashaRemainingYears,
            double currentMahadashaYears)
        {
            if (maha.Planet != birthMahadashaPlanet)
            {
                return GenerateAntardasha(maha, currentMahadashaYears, maha.StartDate, maha.EndDate);
            }

            double fullMahadashaYears = GetYears(maha.Planet);
            double elapsedBeforeBirthYears = fullMahadashaYears - birthMahadashaRemainingYears;
            DateTime fullMahadashaStart = AddDashaYears(maha.StartDate, -elapsedBeforeBirthYears);

            var fullMaha = new MahadashaPeriod
            {
                Planet = maha.Planet,
                StartDate = fullMahadashaStart,
                EndDate = maha.EndDate
            };

            return GenerateAntardasha(fullMaha, fullMahadashaYears, maha.StartDate, maha.EndDate);
        }

        private double GetMahadashaYearsForTimelinePeriod(
            MahadashaPeriod maha,
            Planet birthMahadashaPlanet,
            double birthMahadashaRemainingYears)
        {
            return maha.Planet == birthMahadashaPlanet
                ? birthMahadashaRemainingYears
                : GetYears(maha.Planet);
        }

        private static List<AntardashaPeriod> ClipAntardashas(
            IEnumerable<AntardashaPeriod> antardashas,
            DateTime clipStart,
            DateTime clipEnd)
        {
            return antardashas
                .Where(period => period.EndDate > clipStart && period.StartDate < clipEnd)
                .Select(period => new AntardashaPeriod
                {
                    MainPlanet = period.MainPlanet,
                    SubPlanet = period.SubPlanet,
                    StartDate = period.StartDate < clipStart ? clipStart : period.StartDate,
                    EndDate = period.EndDate > clipEnd ? clipEnd : period.EndDate
                })
                .ToList();
        }

        private static DateTime AddDashaYears(DateTime start, double years)
        {
            int fullYears = (int)Math.Truncate(years);
            double fractionalYears = years - fullYears;

            return start
                .AddYears(fullYears)
                .AddDays(fractionalYears * DaysPerYear);
        }
    }

    public class DashaCalculationResult
    {
        public List<MahadashaPeriod> MahadashaTimeline { get; set; } = new();
        public MahadashaPeriod CurrentMahadasha { get; set; } = new();
        public List<AntardashaPeriod> CurrentAntardashaTimeline { get; set; } = new();
        public AntardashaPeriod CurrentAntardasha { get; set; } = new();
    }
}
