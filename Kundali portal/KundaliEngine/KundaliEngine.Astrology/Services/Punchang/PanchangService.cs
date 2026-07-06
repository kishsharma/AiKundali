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

        public PanchangDetails Calculate(DateTime dateTime, double sunLongitude, double moonLongitude)
        {
            var panchang = new PanchangDetails();

            sunLongitude = Normalize(sunLongitude);
            moonLongitude = Normalize(moonLongitude);

            double diff = Normalize(moonLongitude - sunLongitude);

            // Tithi
            int tithiNumber = (int)(diff / 12) + 1;
            string paksha = tithiNumber <= 15 ? "Shukla" : "Krishna";

            // Nakshatra (call static method directly)
            var nakTuple = NakshatraCalculator.Calculate(moonLongitude);
            string nakshatraName = nakTuple.Nakshatra.ToString();
            int pada = nakTuple.Pada;

            // Yoga
            int yogaIndex = (int)(Normalize(sunLongitude + moonLongitude) / 13.333333333333334);

            // Karana
            int karanaIndex = (int)(diff / 6);

            // Maas (solar month index)
            int sunSign = (int)(sunLongitude / 30);

            panchang.Tithi = GetTithiName(tithiNumber);
            panchang.Paksha = paksha;
            panchang.Nakshatra = nakshatraName;
            panchang.Pada = pada;
            panchang.Yoga = GetYogaName(yogaIndex);
            panchang.Karana = GetKaranaName(karanaIndex);

            // Vaar (weekday)
            panchang.Vaar = dateTime.DayOfWeek switch
            {
                DayOfWeek.Sunday => "Ravivar",
                DayOfWeek.Monday => "Somvar",
                DayOfWeek.Tuesday => "Mangalvar",
                DayOfWeek.Wednesday => "Budhvar",
                DayOfWeek.Thursday => "Guruvar",
                DayOfWeek.Friday => "Shukravar",
                DayOfWeek.Saturday => "Shanivar",
                _ => ""
            };

            // Maas (lunar month) - placeholder, replace with your actual calculation
            panchang.Maas = CalculateLunarMonth(dateTime, sunLongitude, moonLongitude);

            // Shake and Samvat (Vikram Samvat) - placeholder, replace with your actual calculation
            panchang.Shake = CalculateShakeYear(dateTime).ToString();
            panchang.Samvat = CalculateSamvatYear(dateTime).ToString();

            return panchang;
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
            string[] maas = {"Chaitra","Vaishakha","Jyeshtha","Ashadha", "Shravana","Bhadrapada",
                            "Ashwin", "Kartika", "Margashirsha", "Pausha", "Magha", "Phalguna" };

            int idx = Math.Clamp(index, 0, maas.Length - 1);

            return maas[idx];
        }

        // Example placeholder implementations:
        private int CalculateShakeYear(DateTime date)
        {
            // Shaka Samvat = Gregorian Year - 78
            return date.Year - 78;
        }

        private int CalculateSamvatYear(DateTime date)
        {
            // Vikram Samvat = Gregorian Year + 57
            return date.Year + 57;
        }

        private string CalculateLunarMonth(DateTime date, double sunLongitude, double moonLongitude)
        {
            // Implement your actual Maas calculation here
            return "Chaitra"; // Example placeholder
        }
    }
}
