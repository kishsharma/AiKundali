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

            // 🔥 FIRST PERIOD (BALANCE)
            var first = order[startIndex];

            //DateTime firstEnd = currentStart.AddDays(remainingYears * 365.25);

            int fullYears = (int)remainingYears;
            double fractionalYear = remainingYears - fullYears;

            DateTime firstEnd = currentStart.AddYears(fullYears).AddDays(fractionalYear * 365.25);

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

                DateTime end = currentStart.AddYears(dasha.Years);

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


        public List<AntardashaPeriod> GenerateAntardasha(MahadashaPeriod maha, double actualMahadashaYears)
        {
            var result = new List<AntardashaPeriod>();

            var order = DashaRules.VimshottariOrder;

            int startIndex = order.FindIndex(d => d.Planet == maha.Planet);

            DateTime currentStart = maha.StartDate;

            for (int i = 0; i < order.Count; i++)
            {
                var sub = order[(startIndex + i) % order.Count];

                double durationYears = actualMahadashaYears * sub.Years / 120.0;

                int fullYears = (int)durationYears;
                double fractional = durationYears - fullYears;

                DateTime end = currentStart
                    .AddYears(fullYears)
                    .AddDays(fractional * 365.25);

                result.Add(new AntardashaPeriod
                {
                    MainPlanet = maha.Planet,
                    SubPlanet = sub.Planet,
                    StartDate = currentStart,
                    EndDate = end
                });

                currentStart = end;
            }

            return result;
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


    }
}
