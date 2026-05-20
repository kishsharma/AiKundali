using KundaliEngine.Astrology.Astronomy;
using KundaliEngine.Astrology.Calculations;
using KundaliEngine.Astrology.Services;
using KundaliEngine.Domain.Enums;
using KundaliEngine.Domain.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace KundaliEngine.Astrology.Engine
{
    // Simple orchestrator that produces a basic chart using the available calculators.
    //public class ChartGenerator
    //{
    //    private readonly PlanetPositionCalculator _planetCalc;
    //    private readonly LagnaCalculator _lagnaCalc;
    //    private readonly RashiCalculator _rashiCalc;
    //    private readonly HouseCalculator _houseCalc;
    //    private readonly PanchangService _panchangService;

    //    public ChartGenerator()
    //    {
    //        _planetCalc = new PlanetPositionCalculator();
    //        _lagnaCalc = new LagnaCalculator();
    //        _rashiCalc = new RashiCalculator();
    //        _houseCalc = new HouseCalculator();
    //        _panchangService = new PanchangService();

    //    }

    //    // Generate a KundaliChart from birth details
    //    public KundaliChart GenerateChart(BirthDetails details)
    //    {
    //        // Convert local birth time to UTC using provided TimeZoneOffsetHours
    //        DateTime utc = details.BirthDateTimeLocal.Kind == DateTimeKind.Utc
    //            ? details.BirthDateTimeLocal
    //            : details.BirthDateTimeLocal.AddHours(-details.TimeZoneOffsetHours);

    //        var planets = new List<PlanetPosition>();

    //        Planet[] planetsToCalculate = new[]
    //        {
    //            Planet.Sun,
    //            Planet.Moon,
    //            Planet.Mars,
    //            Planet.Mercury,
    //            Planet.Jupiter,
    //            Planet.Venus,
    //            Planet.Saturn,
    //            Planet.Rahu,
    //            Planet.Ketu
    //        };

    //        foreach (var p in planetsToCalculate)
    //        {
    //            planets.Add(_planetCalc.Calculate(p, utc));
    //        }

    //        // Extract Sun & Moon longitudes from the populated planets list
    //        double sun = planets.First(p => p.Planet == Planet.Sun).Longitude;
    //        double moon = planets.First(p => p.Planet == Planet.Moon).Longitude;

    //        // Panchang Calculation
    //        var panchang = _panchangService.Calculate(details.BirthDateTimeLocal, sun, moon);

    //        // Calculate a simple ascendant index (returns rashi index 0..11) using longitude
    //        int ascendantIndex = _lagnaCalc.CalculateLagna(details.Longitude);

    //        // Map ascendant index (0..11) to ZodiacSign enum (1..12)
    //        ZodiacSign ascSign = (ZodiacSign)((ascendantIndex % 12) + 1);

    //        var chart = new KundaliChart
    //        {
    //            BirthDetails = details,
    //            GeneratedAtUtc = DateTime.UtcNow,
    //            AscendantSign = ascSign,
    //            AscendantDegree = 0,
    //            AscendantMinute = 0,
    //            PlanetPositions = planets,
    //            Panchang = panchang
    //        };

    //        // Assign houses to planets using the simple HouseCalculator.GetHouse
    //        foreach (var planet in chart.PlanetPositions)
    //        {
    //            int houseIndex = _houseCalc.GetHouse(planet.Longitude, ascendantIndex);
    //            if (houseIndex < 1) houseIndex = 1;
    //            if (houseIndex > 12) houseIndex = 12;
    //            planet.House = (HouseNumber)houseIndex;
    //        }

    //        return chart;
    //    }
    //}
}
