using KundaliEngine.Astrology.Calculations;
using KundaliEngine.Domain.Models;
using System;

namespace KundaliEngine.Astrology.Services.Panchang
{
    // Service that computes a Panchang for given date/time and Sun/Moon longitudes.
    // Fixed issues:
    // - Interface and implementation signatures aligned
    // - Constructor provides a default NakshatraCalculator for callers that use parameterless construction
    // - Calculate implementation placed inside the class (no stray methods)
    // - Uses PanchangDetails (matches KundaliChart.Panchang) and NakshatraCalculator.Calculate tuple correctly
    public interface IPanchangService
    {
        PanchangDetails Calculate(DateTime dateTime, double sunLongitude, double moonLongitude);
    }

    public class PanchangService : IPanchangService
    {
        public PanchangService()
        {
        }

        public PanchangDetails Calculate(DateTime dateTime, double sun, double moon)
        {
            sun = Normalize(sun);
            moon = Normalize(moon);

            double diff = Normalize(moon - sun);

            // Tithi
            int tithiNumber = (int)(diff / 12) + 1;
            string paksha = tithiNumber <= 15 ? "Shukla" : "Krishna";

            // Nakshatra (call static method directly)
            var nakTuple = NakshatraCalculator.Calculate(moon);
            string nakshatraName = nakTuple.Nakshatra.ToString();
            int pada = nakTuple.Pada;

            // Yoga
            int yogaIndex = (int)(Normalize(sun + moon) / 13.333333333333334);

            // Karana
            int karanaIndex = (int)(diff / 6);

            // Maas (solar month index)
            int sunSign = (int)(sun / 30);

            return new PanchangDetails
            {
                Tithi = GetTithiName(tithiNumber),
                Paksha = paksha,
                Nakshatra = nakshatraName,
                Pada = pada,
                Yoga = GetYogaName(yogaIndex),
                Karana = GetKaranaName(karanaIndex),
                Vaar = dateTime.DayOfWeek.ToString(),
                Maas = GetMaasName(sunSign)
            };
        }

        private double Normalize(double angle)
        {
            return (angle % 360 + 360) % 360;
        }

        private static string GetTithiName(int tithi)
        {
            string[] names = {
                "Prathama","Dvitiya","Tritiya","Chaturthi","Panchami","Shashthi","Saptami","Ashtami","Navami","Dashami",
                "Ekadashi","Dwadashi","Trayodashi","Chaturdashi","Purnima/Amavasya"
            };
            int idx = Math.Clamp(tithi - 1, 0, names.Length - 1);
            return names[idx];
        }

        private static string GetYogaName(int index)
        {

            string[] yogas = new[]
            {
                "Vishkambha","Priti","Ayushman","Saubhagya","Shobhana",
                "Atiganda","Sukarma","Dhriti","Shoola","Ganda",
                "Vriddhi","Dhruva","Vyaghata","Harshana","Vajra",
                "Siddhi","Vyatipata","Variyana","Parigha","Shiva",
                "Siddha","Sadhya","Shubha","Shukla","Brahma",
                "Indra","Vaidhriti"
            };
            string[] names = new string[27];
            for (int i = 0; i < 27; i++) names[i] = $"Yoga-{i + 1}";
            int idx = Math.Clamp(index, 0, names.Length - 1);
            return yogas[idx];
        }

        private static string GetKaranaName(int index)
        {
            // 11 karanas; using placeholders
            string[] names = {
                "Bava","Balava","Kaulava","Taitila","Garaja","Vanija","Visti","Shakuni","Chatushpada","Naga","Kimstughna"
            };
            int idx = Math.Clamp(index, 0, names.Length - 1);
            return names[idx];
        }

        private static string GetMaasName(int index)
        {
            string[] names = {
                "Mesha","Vrishabha","Mithuna","Karka","Simha","Kanya","Tula","Vrishchika","Dhanu","Makara","Kumbha","Meena"
            };
            int idx = Math.Clamp(index, 0, names.Length - 1);
            return names[idx];
        }
    }
}
