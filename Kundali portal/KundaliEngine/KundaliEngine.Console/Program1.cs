using KundaliEngine.Astrology.Astronomy;
using KundaliEngine.Astrology.Calculations;
using KundaliEngine.Astrology.Services;
using KundaliEngine.Astrology.Services.Astrology;
using KundaliEngine.Astrology.Services.Compatibility;
using KundaliEngine.Astrology.Services.Dasha;
using KundaliEngine.Astrology.Services.Prediction;
using KundaliEngine.Astrology.Services.Panchang;
using KundaliEngine.Astrology.Services.Scoring;
using KundaliEngine.Domain.Enums;
using KundaliEngine.Domain.Models;
using KundaliEngine.Domain.Rules;
using KundaliEngine.KnowledgeBase.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

class Program1
{
    static void Main(string[] args)
    {
        Console.WriteLine("Initializing Swiss Ephemeris...");

        string ephePath = Path.Combine(AppContext.BaseDirectory, "ephe");
        IEphemerisProvider ephemerisProvider = new SwissEphemerisProvider(ephePath);

        // Input
        DateTime birthUtc = new DateTime(1986, 10, 6, 6, 15, 0, DateTimeKind.Utc);
        double lat = 19.7115;
        double lon = 77.145;

        // Services / calculators
        var planetCalc = new PlanetPositionCalculator(ephemerisProvider);
        var ascCalc = new AscendantCalculator(ephemerisProvider);
        var panchangService = new PanchangService();
        var houseService = new HouseService();
        var strengthService = new PlanetStrengthService();
        var predictionService = new PredictionService();
        var aspectService = new AspectService();
        var chartService = new ChartService(ephemerisProvider);
        var dashaService = new DashaService();
        var knowledgeService = new AstrologyKnowledgeService();
        var planetScoreService = new PlanetScoreService();
        var houseScoreService = new HouseScoreService();
        var overallKundaliScoreService = new OverallKundaliScoreService();

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

        var panchang = panchangService.Calculate(birthUtc, sunLongitude, moonLongitude);

        Console.WriteLine("===== PANCHANG =====");
        Console.WriteLine($"Tithi    : {panchang.Tithi} ({panchang.Paksha})");
        Console.WriteLine($"Nakshatra: {panchang.Nakshatra} (Pada {panchang.Pada})");
        Console.WriteLine($"Yoga     : {panchang.Yoga}");
        Console.WriteLine($"Karana   : {panchang.Karana}");
        Console.WriteLine($"Vaar     : {panchang.Vaar}");
        Console.WriteLine($"Maas     : {panchang.Maas}");
        Console.WriteLine("====================");
        DisplayPanchang(panchang);

        // Houses
        houseService.AssignWholeSignHouses(ascendantLongitude, planetPositions);

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
                var strength = strengthService.GetStrength(result.Planet, result.Sign);
                var planetKnowledge = knowledgeService.GetPlanet(result.Planet.ToString());
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

                var digbala = strengthService.GetDigbala(result.Planet, result.House);
                Console.WriteLine($"  Digbala   : {digbala}");

                Console.WriteLine("===== PREDICTIONS =====");
                var prediction = predictionService.GetPrediction(result, strength);
                Console.WriteLine(prediction);
            }

            Console.WriteLine("------------------------------------");
        }

        // Aspects
        var aspectResult = aspectService.CalculateGrahaDrishti(planetPositions);

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

        Console.WriteLine("===== HOUSE SCORES =====");
        var houseScores = houseScoreService.CalculateAllHouseScores(
            ascendantSign,
            planetPositions,
            aspectResult);

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

        // ChartService (higher-level orchestration)
        var finalPlanets = chartService.GenerateChart(birthUtc, lat, lon);
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
        var birthDasha = dashaService.GetBirthMahadasha(moonPos);

        Console.WriteLine("===== BIRTH DASHA =====");
        Console.WriteLine($"Starting Mahadasha : {birthDasha.Planet}");
        Console.WriteLine($"Balance at Birth   : {birthDasha.RemainingYears:F2} years");
        Console.WriteLine("========================");

        var mahadashaTimeline = dashaService.GenerateMahadashaTimeline(birthDasha.Planet, birthDasha.RemainingYears, birthUtc);
        Console.WriteLine("===== MAHADASHA TIMELINE =====");
        foreach (var d in mahadashaTimeline)
        {
            Console.WriteLine($"{d.Planet} : {d.StartDate:yyyy-MM-dd} → {d.EndDate:yyyy-MM-dd}");
        }

        // Determine current maha
        DateTime nowUtc = DateTime.UtcNow;
        var currentMaha = mahadashaTimeline.FirstOrDefault(m => nowUtc >= m.StartDate && nowUtc < m.EndDate) ?? mahadashaTimeline.Last();

        var mahaYears = DashaRules.VimshottariOrder.First(d => d.Planet == currentMaha.Planet).Years;
        var antarTimeline = dashaService.GenerateAntardasha(currentMaha, mahaYears);

        Console.WriteLine("===== ANTARDASHA =====");
        foreach (var a in antarTimeline)
        {
            Console.WriteLine($"{a.MainPlanet} → {a.SubPlanet} : {a.StartDate:yyyy-MM-dd} → {a.EndDate:yyyy-MM-dd}");
        }

        var current = dashaService.GetCurrentDasha(mahadashaTimeline, antarTimeline, nowUtc);
        Console.WriteLine("===== CURRENT DASHA =====");
        Console.WriteLine($"Maha  : {current.Maha}");
        Console.WriteLine($"Antar : {current.Antar}");

        Console.WriteLine("===== OVERALL KUNDALI SCORE =====");
        var overallScore = overallKundaliScoreService.Calculate(
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

        string mahaStaticPrediction =
            predictionService.GetDashaPrediction(current.Maha);

        string antarPrediction =
            predictionService.GetDashaPrediction(current.Antar);

        Console.WriteLine($"{current.Maha} Mahadasha {mahaStaticPrediction}");

        Console.WriteLine();

        Console.WriteLine($"{current.Antar} Antardasha {antarPrediction}");

        Console.WriteLine("========================================");
        Console.WriteLine("===== DASHA ACTIVATED PREDICTION =====");
        var mahaPlanetPosition = planetPositions.FirstOrDefault(p => p.Planet == current.Maha);

        if (mahaPlanetPosition != null)
        {
            PlanetStrength mahaActiveStrength = strengthService.GetStrength(mahaPlanetPosition.Planet, mahaPlanetPosition.Sign);

            string mahaActivePrediction = predictionService.GetDashaActivatedPrediction(mahaPlanetPosition, current.Maha, mahaActiveStrength);
            Console.WriteLine(mahaActivePrediction);
        }
        else
        {
            Console.WriteLine($"No planet position found for current Mahadasha planet: {current.Maha}");
        }
        Console.WriteLine();
        Console.WriteLine("=====================================");

        Console.WriteLine("===== COMBINED DASHA SYNTHESIS =====");

        string combined = predictionService.GetCombinedDashaPrediction(current.Maha, current.Antar);

        Console.WriteLine(combined);

        Console.WriteLine("====================================");
        
        // ====================================================
        // PHASE 1: COMPATIBILITY MATCHMAKING DEMONSTRATION
        // ====================================================
        
        Console.WriteLine("\n");
        Console.WriteLine("╔════════════════════════════════════════════════════════════╗");
        Console.WriteLine("║          PHASE 1: COMPATIBILITY MATCHMAKING                 ║");
        Console.WriteLine("║        (Foundation Architecture Demonstration)              ║");
        Console.WriteLine("╚════════════════════════════════════════════════════════════╝");

        // Generate a second Kundali (for demonstration: same chart as first person)
        var chartService2 = new ChartService(ephemerisProvider);

       
        var person1 = chartService.GenerateChart(birthUtc, lat, lon);
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
            Panchang = panchang
        };

        DateTime birthUtcPerson2 = new DateTime(1991, 12, 7, 6, 15, 0, DateTimeKind.Utc);
        double latPerson2 = 24.076;
        double lonPerson2 = 75.069;
        var person2 = chartService2.GenerateChart(birthUtcPerson2, latPerson2, lonPerson2);


        // Ascendant
        double ascendantLongitudePerson2 = ascCalc.Calculate(birthUtcPerson2, latPerson2, lonPerson2);
        int ascendantIndexPerson2 = (int)(ascendantLongitude / 30.0);
        ZodiacSign ascendantSignPerson2 = (ZodiacSign)(ascendantIndex + 1);

        var planetPositionsperson2 = planets
            .Select(p => planetCalc.Calculate(p, birthUtcPerson2))
            .ToList();

        // Aspects
        var aspectResultperson2 = aspectService.CalculateGrahaDrishti(planetPositionsperson2);

        // Panchang
        double sunLongitudeP2 = planetPositionsperson2.First(p => p.Planet == Planet.Sun).Longitude;
        double moonLongitudeP2 = planetPositionsperson2.First(p => p.Planet == Planet.Moon).Longitude;

        var panchangP2 = panchangService.Calculate(birthUtcPerson2, sunLongitudeP2, moonLongitudeP2);

        KundaliChart kundaliChart2 = new KundaliChart
        {
            BirthDetails = new BirthDetails
            {
                BirthDateTimeLocal = birthUtcPerson2, // Assuming birthUtc is local for simplicity
                Latitude = latPerson2,
                Longitude = lonPerson2,
                TimeZoneOffsetHours = 0 // Assuming UTC for simplicity
            },
            AscendantDegree = (int)(ascendantLongitudePerson2 % 30),
            AscendantMinute = (int)((ascendantLongitudePerson2 % 1) * 60),
            AscendantSign = ascendantSignPerson2,
            Aspects = aspectResultperson2.PlanetAspects,
            Panchang = panchangP2
        };
        // Create compatibility profiles
        var profile1 = new CompatibilityProfile("Kishore",kundaliChart1);
        var profile2 = new CompatibilityProfile("Pooja", kundaliChart2);

        // Calculate compatibility
        var compatibilityService = new CompatibilityScoreService();
        var compatibilityResult = compatibilityService.CalculateCompatibility(profile1, profile2);

        // Display compatibility result
        Console.WriteLine($"\n===== COMPATIBILITY ANALYSIS =====");
        Console.WriteLine($"Persons: {compatibilityResult.PersonAName} & {compatibilityResult.PersonBName}");
        Console.WriteLine($"Overall Score: {compatibilityResult.OverallCompatibilityScore}/100");
        Console.WriteLine($"Confidence: {compatibilityResult.OverallConfidence}%");

        Console.WriteLine("\n--- Compatibility Dimensions ---");
        foreach (var dimension in compatibilityResult.Dimensions)
        {
            Console.WriteLine($"\n{dimension.Name}: {dimension.Score}/100 (Confidence: {dimension.Confidence}%)");
            if (dimension.Factors.Count > 0)
            {
                foreach (var factor in dimension.Factors)
                {
                    Console.WriteLine($"  {factor}");
                }
            }
            if (!string.IsNullOrEmpty(dimension.Summary))
            {
                Console.WriteLine($"  Summary: {dimension.Summary}");
            }
        }

        Console.WriteLine("\n--- Dominant Strengths ---");
        foreach (var strength in compatibilityResult.DominantStrengths)
        {
            Console.WriteLine($"  + {strength}");
        }

        Console.WriteLine("\n--- Dominant Challenges ---");
        foreach (var challenge in compatibilityResult.DominantChallenges)
        {
            Console.WriteLine($"  - {challenge}");
        }

        Console.WriteLine("\n--- Overall Summary ---");
        Console.WriteLine(compatibilityResult.Summary);

        Console.WriteLine($"\nAnalyzed at: {compatibilityResult.AnalyzedAtUtc:yyyy-MM-dd HH:mm:ss} UTC");
        Console.WriteLine($"Engine Version: {compatibilityResult.EngineVersion}");

        Console.WriteLine("\n════════════════════════════════════════════════════════════");
        Console.WriteLine("Phase 1 compatibility architecture complete.");
        Console.WriteLine("Ready for future compatibility analyzers (Moon, Nakshatra, Venus/Mars, etc.)");
        Console.WriteLine("════════════════════════════════════════════════════════════");

        Console.ReadLine();
    }
}
