using System;
using System.IO;
using KundaliEngine.Astrology.Astronomy;
using KundaliEngine.Domain.Enums;

class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("Initializing Swiss Ephemeris...");

        string ephePath = Path.Combine(AppContext.BaseDirectory, "ephe");
        SwissEphWrapper.Initialize(ephePath);

        var calculator = new PlanetPositionCalculator();
        DateTime birthUtc = new DateTime(2026, 4, 18, 13, 30, 0, DateTimeKind.Utc);

        Console.WriteLine("Calculating planetary positions...\n");

        Planet[] planets =
        {
            Planet.Sun,
            Planet.Moon,
            Planet.Mars,
            Planet.Mercury,
            Planet.Jupiter,
            Planet.Venus,
            Planet.Saturn,
            Planet.Rahu,
            Planet.Ketu
        };

        foreach (var planet in planets)
        {
            var result = calculator.Calculate(planet, birthUtc);

            Console.WriteLine($"{result.Planet}");
            Console.WriteLine($"  Sign     : {result.Sign}");
            Console.WriteLine($"  Position : {result.Degree}° {result.Minute}'");
            Console.WriteLine($"  Nakshatra: {result.Nakshatra}");
            Console.WriteLine($"  Pada     : {result.Pada}");
            Console.WriteLine($"  Longitude: {result.Longitude}");
            Console.WriteLine($"  Latitude : {result.Latitude}");
            Console.WriteLine("------------------------------------");
        }

        Console.WriteLine("Calculation Complete.");
        Console.ReadLine();
    }
}
