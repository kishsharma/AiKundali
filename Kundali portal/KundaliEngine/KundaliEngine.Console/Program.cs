using KundaliEngine.Astrology.Astronomy;
using KundaliEngine.Astrology.Services;
using KundaliEngine.Domain.Enums;
using KundaliEngine.Domain.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("Initializing Swiss Ephemeris...");

        string ephePath = Path.Combine(AppContext.BaseDirectory, "ephe");
        SwissEphWrapper.Initialize(ephePath);

        var calculator = new PlanetPositionCalculator();
        DateTime birthUtc = new DateTime(1986, 10, 6, 6, 15, 0, DateTimeKind.Utc);
        double lat = 19.7115;
        double lon = 77.145;

        var ascendantCalculator = new AscendantCalculator();
        double ascendantLongitude = ascendantCalculator.Calculate(birthUtc, lat, lon);
        int ascendantIndex = (int)(ascendantLongitude / 30.0);
        ZodiacSign ascendantSign = (ZodiacSign)(ascendantIndex + 1);

        Console.WriteLine("===== ASCENDANT =====");
        Console.WriteLine($"Ascendant Longitude: {ascendantLongitude}");
        Console.WriteLine($"Ascendant Index: {ascendantIndex}");
        Console.WriteLine($"Ascendant Sign: {(int)ascendantSign} - {ascendantSign}");
        Console.WriteLine("======================");

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

        var planetPositions = new List<PlanetPosition>();
        foreach (var planet in planets)
        {
            planetPositions.Add(calculator.Calculate(planet, birthUtc));
        }

        var houseService = new HouseService();
        houseService.AssignWholeSignHouses(ascendantLongitude, planetPositions);

        planetPositions = planetPositions
            .OrderBy(position => position.House)
            .ToList();

        for (int houseNumber = 1; houseNumber <= 12; houseNumber++)
        {
            HouseNumber currentHouse = (HouseNumber)houseNumber;
            var housePlanets = planetPositions
                .Where(position => position.House == currentHouse)
                .ToList();

            Console.WriteLine("Start of other data");
            Console.WriteLine($"House    : {currentHouse}");
            var strengthService = new PlanetStrengthService();
            

            foreach (var result in housePlanets)
            {

                var strength = strengthService.GetStrength(result.Planet, result.Sign);
                Console.WriteLine($"{result.Planet}");
                Console.WriteLine($"Strength : {strength}");
                Console.WriteLine($"  Sign     : {(int)result.Sign} - {result.Sign}");
                Console.WriteLine($"  Position : {result.Degree}° {result.Minute}'");

                Console.WriteLine($"  Nakshatra: {result.Nakshatra}");
                Console.WriteLine($"  Pada     : {result.Pada}");
                Console.WriteLine($"  Longitude: {result.Longitude}");
                Console.WriteLine($"  Latitude : {result.Latitude}");

                

                
            }

            Console.WriteLine("------------------------------------");
        }

        var aspectService = new AspectService();
        var aspectResult = aspectService.CalculateGrahaDrishti(planetPositions);

        var houseAspects = aspectResult.HouseAspects
            .OrderBy(aspect => aspect.FromPlanet)
            .ThenBy(aspect => aspect.AspectOffset)
            .ThenBy(aspect => aspect.ToHouse)
            .ToList();

        Console.WriteLine("===== HOUSE ASPECTS =====");
        foreach (var aspect in houseAspects)
        {
            Console.WriteLine($"{aspect.FromPlanet} -> House {aspect.ToHouse} ({aspect.AspectOffset} aspect)");
        }
        Console.WriteLine("=========================");

        var planetAspects = aspectResult.PlanetAspects
            .OrderBy(aspect => aspect.FromPlanet)
            .ThenBy(aspect => aspect.AspectOffset)
            .ThenBy(aspect => aspect.ToPlanet)
            .ToList();

        Console.WriteLine("===== PLANET ASPECTS =====");
        foreach (var aspect in planetAspects)
        {
            Console.WriteLine($"{aspect.FromPlanet} -> {aspect.ToPlanet} ({aspect.AspectOffset} aspect)");
        }
        Console.WriteLine("==========================");

        Console.WriteLine("Calculation Complete.");
       // Console.ReadLine();



        Console.WriteLine("========Something new==================");
        var chartService = new ChartService();

        var finalplanets = chartService.GenerateChart(birthUtc, lat, lon);

        foreach (var result in finalplanets)
        {
            Console.WriteLine($"{result.Planet}");
            Console.WriteLine($"  Strength : {result.DetailedStrength}");

            if (result.IsCombust)
                Console.WriteLine($"  Status   : Combust");

            Console.WriteLine("------------------------------------");
        }
        Console.ReadLine();
    }
}
