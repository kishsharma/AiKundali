using KundaliEngine.Astrology.Astronomy;
using KundaliEngine.Astrology.Calculations;
using KundaliEngine.Astrology.Services;
using KundaliEngine.Astrology.Services.Astrology;
using KundaliEngine.Astrology.Services.Compatibility;
using KundaliEngine.Astrology.Services.Dasha;
using KundaliEngine.Astrology.Services.DivisionalCharts;
using KundaliEngine.Astrology.Services.Panchang;
using KundaliEngine.Astrology.Services.Prediction;
using KundaliEngine.Astrology.Services.Scoring;
using KundaliEngine.Console;
using KundaliEngine.Domain.Enums;
using KundaliEngine.Domain.Models;
using KundaliEngine.Domain.Models.DivisionalCharts;
using KundaliEngine.Domain.Rules;
using KundaliEngine.KnowledgeBase.Services;
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
        AppServices appServices = new AppServices();
        // Input

        DateTime localBirth = new DateTime(1986, 10, 6, 11, 45, 0);

        TimeZoneInfo indiaZone =      TimeZoneInfo.FindSystemTimeZoneById("India Standard Time");

        DateTime birthUtc =  TimeZoneInfo.ConvertTimeToUtc(localBirth, indiaZone);

        Console.WriteLine(birthUtc);
        //DateTime birthUtc = new DateTime(1986, 10, 6, 11, 45, 0, DateTimeKind.Utc);
        double lat = 19.5431164;
        double lon = 77.1739432;

        // Services / calculators
        var planetCalc = new PlanetPositionCalculator();
        var ascCalc = new AscendantCalculator();
        //var panchangService = new PanchangService();
        //var houseService = new HouseService();
        //var strengthService = new PlanetStrengthService();
        //var predictionService = new PredictionService();
        //var aspectService = new AspectService();
        //var chartService = new ChartService();
        //var dashaService = new DashaService();
        //var knowledgeService = new AstrologyKnowledgeService();
        //var planetScoreService = new PlanetScoreService();
        //var houseScoreService = new HouseScoreService();
        //var overallKundaliScoreService = new OverallKundaliScoreService();

        var services = new AppServices();

        // Ascendant
        double ascendantLongitude = ascCalc.Calculate(birthUtc, lat, lon);
        int ascendantIndex = (int)(ascendantLongitude / 30.0);
        ZodiacSign ascendantSign = (ZodiacSign)(ascendantIndex + 1);

        Console.WriteLine("===== ASCENDANT =====");
        Console.WriteLine($"Ascendant Longitude: {ascendantLongitude}");
        Console.WriteLine($"Ascendant Index: {ascendantIndex}");
        Console.WriteLine($"Ascendant Sign: {(int)ascendantSign} - {ascendantSign}");
        Console.WriteLine("======================");

        // Calculate planet positions
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

        var planetPositions = planets
            .Select(p => planetCalc.Calculate(p, birthUtc))
            .ToList();

        // Panchang
        double sunLongitude = planetPositions.First(p => p.Planet == Planet.Sun).Longitude;
        double moonLongitude = planetPositions.First(p => p.Planet == Planet.Moon).Longitude;

        var panchang = services.PanchangService.Calculate(birthUtc, sunLongitude, moonLongitude);

        //Console.WriteLine("===== PANCHANG =====");
        //Console.WriteLine($"Tithi    : {panchang.Tithi} ({panchang.Paksha})");
        //Console.WriteLine($"Nakshatra: {panchang.Nakshatra} (Pada {panchang.Pada})");
        //Console.WriteLine($"Yoga     : {panchang.Yoga}");
        //Console.WriteLine($"Karana   : {panchang.Karana}");
        //Console.WriteLine($"Vaar     : {panchang.Vaar}");
        //Console.WriteLine($"Maas     : {panchang.Maas}");
        //Console.WriteLine("====================");

        DisplayPanchang(panchang);

        // Houses
        services.HouseService.AssignWholeSignHouses(ascendantLongitude, planetPositions);

        var orderedByHouse = planetPositions
            .OrderBy(p => p.House)
            .ToList();

        // Display per-house summary
        for (int houseNumber = 1; houseNumber <= 12; houseNumber++)
        {
            HouseNumber currentHouse = (HouseNumber)houseNumber;
            var housePlanets = orderedByHouse
                .Where(position => position.House == currentHouse)
                .ToList();

            Console.WriteLine("Start of other data");
            Console.WriteLine($"House    : {currentHouse}");

            foreach (var result in housePlanets)
            {
                var strength = services.StrengthService.GetStrength(result.Planet, result.Sign);
                var planetKnowledge = services.KnowledgeService.GetPlanet(result.Planet.ToString());
                Console.WriteLine($"{result.Planet}");
                Console.WriteLine($"Strength : {strength}");
                if (planetKnowledge != null)
                {
                    Console.WriteLine($"  Nature   : {planetKnowledge.Nature}");
                    Console.WriteLine($"  Karakatva: {string.Join(", ", planetKnowledge.Karakatva)}");
                }
                Console.WriteLine($"  Sign     : {(int)result.Sign} - {result.Sign}");
                Console.WriteLine($"  Position : {result.Degree}° {result.Minute}'");
                Console.WriteLine($"  Nakshatra: {result.Nakshatra}");
                Console.WriteLine($"  Pada     : {result.Pada}");
                Console.WriteLine($"  Longitude: {result.Longitude}");
                Console.WriteLine($"  Latitude : {result.Latitude}");

                var digbala = services.StrengthService.GetDigbala(result.Planet, result.House);
                Console.WriteLine($"  Digbala   : {digbala}");

                Console.WriteLine("===== PREDICTIONS =====");
                var prediction =services.PredictionService.GetPrediction(result, strength);
                Console.WriteLine(prediction);
            }

            Console.WriteLine("------------------------------------");
        }

        // Aspects
        var aspectResult =services.AspectService.CalculateGrahaDrishti(planetPositions);

        var houseAspects = aspectResult.HouseAspects
            .OrderBy(a => a.FromPlanet)
            .ThenBy(a => a.AspectOffset)
            .ThenBy(a => a.ToHouse)
            .ToList();

        Console.WriteLine("===== HOUSE ASPECTS =====");
        foreach (var aspect in houseAspects)
        {
            Console.WriteLine($"{aspect.FromPlanet} -> House {aspect.ToHouse} ({aspect.AspectOffset} aspect)");
        }
        Console.WriteLine("=========================");

        var planetAspects = aspectResult.PlanetAspects
            .OrderBy(a => a.FromPlanet)
            .ThenBy(a => a.AspectOffset)
            .ThenBy(a => a.ToPlanet)
            .ToList();

        Console.WriteLine("===== PLANET ASPECTS =====");
        foreach (var aspect in planetAspects)
        {
            Console.WriteLine($"{aspect.FromPlanet} -> {aspect.ToPlanet} ({aspect.AspectOffset} aspect)");
        }
        Console.WriteLine("==========================");

        // Combustion
        foreach (var p in planetPositions)
        {
            p.IsCombust = CombustionCalculator.IsCombust(p.Planet, p.Longitude, sunLongitude);
        }

        //Console.WriteLine("===== HOUSE SCORES =====");
        var houseScores = services.HouseScoreService.CalculateAllHouseScores( ascendantSign,planetPositions,aspectResult);

        //foreach (var result in houseScores)
        //{
        //    Console.WriteLine($"{result.HouseName} ({result.HouseNumber}) : {result.Score}/100");

        //    if (result.DominantThemes.Count > 0)
        //    {
        //        Console.WriteLine($"  Themes: {string.Join(", ", result.DominantThemes)}");
        //    }

        //    foreach (var factor in result.Factors)
        //    {
        //        Console.WriteLine($"  {factor}");
        //    }

        //    Console.WriteLine("--------------------------------");
        //}
        DisplayHouseScores(houseScores);
        //Console.WriteLine("===== PLANET SCORES =====");
        //foreach (var planet in planetPositions)
        //{
        //    var result =services.PlanetScoreService.Calculate(planet);

        //    Console.WriteLine($"{result.Planet} : {result.Score}/100");

        //    foreach (var factor in result.Factors)
        //    {
        //        Console.WriteLine($"  {factor}");
        //    }

        //    Console.WriteLine("--------------------------------");
        //}

        DisplayPlanetScores(planetPositions, services.PlanetScoreService);

        // ChartService (higher-level orchestration)
        var finalPlanets = services.ChartService.GenerateChart(birthUtc, lat, lon);
        Console.WriteLine("======== ChartService Output ========");
        foreach (var result in finalPlanets)
        {
            Console.WriteLine($"{result.Planet}");
            Console.WriteLine($"  Strength : {result.DetailedStrength}");
            Console.WriteLine($"  Retrograde : {result.IsRetrograde}");
            Console.WriteLine($"  Combust    : {result.IsCombust}");
            if (result.IsCombust)
                Console.WriteLine($"  Status   : Combust");
            Console.WriteLine("------------------------------------");
        }

        // Dasha
        var moonPos = planetPositions.First(p => p.Planet == Planet.Moon);
        var birthDasha =services.DashaService.GetBirthMahadasha(moonPos);

        Console.WriteLine("===== BIRTH DASHA =====");
        Console.WriteLine($"Starting Mahadasha : {birthDasha.Planet}");
        Console.WriteLine($"Balance at Birth   : {birthDasha.RemainingYears:F2} years");
        Console.WriteLine("========================");

        var mahadashaTimeline = services.DashaService.GenerateMahadashaTimeline(birthDasha.Planet, birthDasha.RemainingYears, birthUtc);
        Console.WriteLine("===== MAHADASHA TIMELINE =====");
        foreach (var d in mahadashaTimeline)
        {
            Console.WriteLine($"{d.Planet} : {d.StartDate:yyyy-MM-dd} → {d.EndDate:yyyy-MM-dd}");
        }

        // Determine current maha
        DateTime nowUtc = DateTime.UtcNow;
        var currentMaha = mahadashaTimeline.FirstOrDefault(m => nowUtc >= m.StartDate && nowUtc < m.EndDate) ?? mahadashaTimeline.Last();

        var mahaYears = DashaRules.VimshottariOrder.First(d => d.Planet == currentMaha.Planet).Years;
        var antarTimeline = services.DashaService.GenerateAntardasha(currentMaha, mahaYears);

        Console.WriteLine("===== ANTARDASHA =====");
        foreach (var a in antarTimeline)
        {
            Console.WriteLine($"{a.MainPlanet} → {a.SubPlanet} : {a.StartDate:yyyy-MM-dd} → {a.EndDate:yyyy-MM-dd}");
        }

        var current = services.DashaService.GetCurrentDasha(mahadashaTimeline, antarTimeline, nowUtc);
        Console.WriteLine("===== CURRENT DASHA =====");
        Console.WriteLine($"Maha  : {current.Maha}");
        Console.WriteLine($"Antar : {current.Antar}");

        Console.WriteLine("===== OVERALL KUNDALI SCORE =====");
        var overallScore = services.OverallKundaliScoreService.Calculate(
            ascendantSign,
            planetPositions,
            aspectResult,
            current.Maha);

        Console.WriteLine($"Overall   : {overallScore.OverallScore}/100");
        Console.WriteLine($"Stability : {overallScore.StabilityScore}/100");
        Console.WriteLine($"Growth    : {overallScore.GrowthScore}/100");
        Console.WriteLine($"Challenge : {overallScore.ChallengeScore}/100");

        Console.WriteLine("Dominant Strengths:");
        foreach (var strength in overallScore.DominantStrengths)
        {
            Console.WriteLine($"  + {strength}");
        }

        Console.WriteLine("Dominant Weaknesses:");
        foreach (var weakness in overallScore.DominantWeaknesses)
        {
            Console.WriteLine($"  - {weakness}");
        }

        Console.WriteLine("Factors:");
        foreach (var factor in overallScore.Factors)
        {
            Console.WriteLine($"  {factor}");
        }

        Console.WriteLine("=================================");


        Console.WriteLine("=========================");
        Console.WriteLine("===== CURRENT DASHA INTERPRETATION =====");

        string mahaStaticPrediction =services.PredictionService.GetDashaPrediction(current.Maha);

        string antarPrediction =services.PredictionService.GetDashaPrediction(current.Antar);

        Console.WriteLine($"{current.Maha} Mahadasha {mahaStaticPrediction}");

        Console.WriteLine();

        Console.WriteLine($"{current.Antar} Antardasha {antarPrediction}");

        Console.WriteLine("========================================");
        Console.WriteLine("===== DASHA ACTIVATED PREDICTION =====");
        var mahaPlanetPosition = planetPositions.FirstOrDefault(p => p.Planet == current.Maha);

        if (mahaPlanetPosition != null)
        {
            PlanetStrength mahaActiveStrength =services.StrengthService.GetStrength(mahaPlanetPosition.Planet, mahaPlanetPosition.Sign);

            string mahaActivePrediction = services.PredictionService.GetDashaActivatedPrediction(mahaPlanetPosition, current.Maha, mahaActiveStrength);
            Console.WriteLine(mahaActivePrediction);
        }
        else
        {
            Console.WriteLine($"No planet position found for current Mahadasha planet: {current.Maha}");
        }
        Console.WriteLine();
        Console.WriteLine("=====================================");

        Console.WriteLine("===== COMBINED DASHA SYNTHESIS =====");

        string combined =services.PredictionService.GetCombinedDashaPrediction(current.Maha, current.Antar);

        Console.WriteLine(combined);

        Console.WriteLine("====================================");

        
        Console.WriteLine();
        Console.WriteLine("====================================");
        Console.WriteLine("PHASE 1: COMPATIBILITY FOUNDATION");
        Console.WriteLine("====================================");

        var profile1 = BuildCompatibilityProfile("Kishore", birthUtc, lat,lon, services);

        var profile2 = BuildCompatibilityProfile("Pooja",new DateTime(1991, 12, 7, 6, 15, 0, DateTimeKind.Utc),24.076, 75.069,services);

        var compatibilityResult = services.CompatibilityService.CalculateCompatibility( profile1,profile2);

        DisplayCompatibilityResult(compatibilityResult);

        var divisionalChartService =new DivisionalChartService();

        // Generate a second Kundali (for demonstration: same chart as first person)
       
        var person1 = services.ChartService.GenerateChart(birthUtc, lat, lon);

        //var planetPositions = services.ChartService.GenerateChart(
        //  birthUtc,
        //  lat,
        //  lon);

        KundaliChart kundaliChart1 = new KundaliChart
        {
            BirthDetails = new BirthDetails
            {
                BirthDateTimeLocal = birthUtc, // Assuming birthUtc is local for simplicity
                Latitude = lat,
                Longitude = lon,
                TimeZoneOffsetHours = 0 // Assuming UTC for simplicity
            },
            AscendantDegree = (int)(ascendantLongitude % 30),
            AscendantMinute = (int)((ascendantLongitude % 1) * 60),
            AscendantSign = ascendantSign,
            Aspects = aspectResult.PlanetAspects,
            Panchang = panchang,
            PlanetPositions= person1
        };

        //var d9Chart = divisionalChartService.GenerateDivisionalChart(kundaliChart1, DivisionalChartType.D9);

        //DisplayDivisionalChart(d9Chart);

        RunDivisionalChartTests(kundaliChart1);

        Console.ReadLine();
    }

    static void InitializeSwissEphemeris()
    {
        Console.WriteLine("Initializing Swiss Ephemeris...");

        string ephePath = Path.Combine(AppContext.BaseDirectory, "ephe");

        SwissEphWrapper.Initialize(ephePath);
    }

    static void DisplayPanchang(PanchangDetails panchang)
    {
        Console.WriteLine("===== PANCHANG =====");

        Console.WriteLine($"Tithi    : {panchang.Tithi} ({panchang.Paksha})");
        Console.WriteLine($"Nakshatra: {panchang.Nakshatra} (Pada {panchang.Pada})");
        Console.WriteLine($"Yoga     : {panchang.Yoga}");
        Console.WriteLine($"Karana   : {panchang.Karana}");
        Console.WriteLine($"Vaar     : {panchang.Vaar}");
        Console.WriteLine($"Maas     : {panchang.Maas}");

        Console.WriteLine("====================");
    }

    static void DisplayPlanetScores( List<PlanetPosition> planetPositions, PlanetScoreService planetScoreService)
    {
        Console.WriteLine("===== PLANET SCORES =====");

        foreach (var planet in planetPositions)
        {
            var result = planetScoreService.Calculate(planet);

            Console.WriteLine($"{result.Planet} : {result.Score}/100");

            foreach (var factor in result.Factors)
            {
                Console.WriteLine($"  {factor}");
            }

            Console.WriteLine("--------------------------------");
        }
    }

    static void DisplayHouseScores(List<HouseScoreResult> houseScores)
    {
        Console.WriteLine("===== HOUSE SCORES =====");

        foreach (var result in houseScores)
        {
            Console.WriteLine($"{result.HouseName} ({result.HouseNumber}) : {result.Score}/100");

            if (result.DominantThemes.Count > 0)
            {
                Console.WriteLine($"  Themes: {string.Join(", ", result.DominantThemes)}");
            }

            foreach (var factor in result.Factors)
            {
                Console.WriteLine($"  {factor}");
            }

            Console.WriteLine("--------------------------------");
        }
    }

    static void DisplayCompatibilityResult(CompatibilityResult compatibilityResult)
    {
        Console.WriteLine("\n===== COMPATIBILITY FOUNDATION =====");

        Console.WriteLine(
            $"Persons: {compatibilityResult.PersonAName} & {compatibilityResult.PersonBName}");

        Console.WriteLine(
            $"Overall Score: {compatibilityResult.OverallCompatibilityScore}/100");

        Console.WriteLine("\n--- Compatibility Dimensions ---");

        foreach (var dimension in compatibilityResult.Dimensions)
        {
            Console.WriteLine(
                $"\n{dimension.Name}: {dimension.Score}/100");

            if (!string.IsNullOrWhiteSpace(dimension.Summary))
            {
                Console.WriteLine(
                    $"  Summary: {dimension.Summary}");
            }
        }

        Console.WriteLine("\n====================================");
    }

    static CompatibilityProfile BuildCompatibilityProfile(string name,  DateTime birthUtc,  double lat, double lon, AppServices services)
    {
        // Generate planets
        var planetPositions = services.ChartService.GenerateChart(
            birthUtc,
            lat,
            lon);

        // Ascendant
        var ascCalc = new AscendantCalculator();

        double ascendantLongitude =
            ascCalc.Calculate(birthUtc, lat, lon);

        int ascendantIndex =
            (int)(ascendantLongitude / 30.0);

        ZodiacSign ascendantSign =
            (ZodiacSign)(ascendantIndex + 1);

        // Panchang
        double sunLongitude =
            planetPositions.First(p => p.Planet == Planet.Sun).Longitude;

        double moonLongitude =
            planetPositions.First(p => p.Planet == Planet.Moon).Longitude;

        var panchang = services.PanchangService.Calculate(
            birthUtc,
            sunLongitude,
            moonLongitude);

        // Aspects
        var aspectResult =
            services.AspectService.CalculateGrahaDrishti(
                planetPositions);

        // Build KundaliChart
        var kundaliChart = new KundaliChart
        {
            BirthDetails = new BirthDetails
            {
                BirthDateTimeLocal = birthUtc,
                Latitude = lat,
                Longitude = lon,
                TimeZoneOffsetHours = 0
            },

            AscendantDegree = (int)(ascendantLongitude % 30),

            AscendantMinute =
                (int)((ascendantLongitude % 1) * 60),

            AscendantSign = ascendantSign,

            Aspects = aspectResult.PlanetAspects,

            Panchang = panchang
        };

        // Return profile
        return new CompatibilityProfile(
            name,
            kundaliChart);
    }

    static void DisplayDivisionalChart( DivisionalChart chart)
    {
        Console.WriteLine();
        Console.WriteLine("====================================");
        Console.WriteLine($"DIVISIONAL CHART : {chart.ChartType}");
        Console.WriteLine("====================================");

        foreach (var planet in chart.PlanetPositions)
        {
            Console.WriteLine($"{planet.Planet}");

            Console.WriteLine($"  Sign      : {planet.Sign}");

            Console.WriteLine($"  Degree    : {planet.Degree}° {planet.Minute}'");

            Console.WriteLine($"  Nakshatra : {planet.Nakshatra}");

            Console.WriteLine($"  Pada      : {planet.Pada}");

            Console.WriteLine("--------------------------------");
        }

        Console.WriteLine("====================================");
    }

    static void RunDivisionalChartTests( KundaliChart kundaliChart)
    {
        Console.WriteLine();
        Console.WriteLine("====================================");
        Console.WriteLine("DIVISIONAL CHART TESTING");
        Console.WriteLine("====================================");
       
        var divisionalService =
            new DivisionalChartService();

        // Test D9
        var d9Chart = divisionalService.GenerateDivisionalChart(kundaliChart, DivisionalChartType.D9);

        DisplayDivisionalChart(d9Chart);

        // Test D10
        var d10Chart = divisionalService.GenerateDivisionalChart( kundaliChart,DivisionalChartType.D10);

        DisplayDivisionalChart(d10Chart);

        Console.WriteLine();
        Console.WriteLine("Divisional chart testing completed.");
        Console.WriteLine("====================================");
    }
    static void CompareD1AndD9(KundaliChart d1Chart,  DivisionalChart d9Chart)
    {
        Console.WriteLine();
        Console.WriteLine("====================================");
        Console.WriteLine("D1 vs D9 COMPARISON");
        Console.WriteLine("====================================");

        foreach (var d1Planet in d1Chart.PlanetPositions)
        {
            var d9Planet =
                d9Chart.PlanetPositions
                    .First(p => p.Planet == d1Planet.Planet);

            Console.WriteLine(
                $"{d1Planet.Planet}");

            Console.WriteLine(
                $"  D1 : {d1Planet.Sign}");

            Console.WriteLine(
                $"  D9 : {d9Planet.Sign}");

            Console.WriteLine("--------------------------------");
        }

        Console.WriteLine("====================================");
    }
}
