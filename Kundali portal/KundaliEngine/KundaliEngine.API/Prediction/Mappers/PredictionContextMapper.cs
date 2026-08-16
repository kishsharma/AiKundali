using HoraLife.PredictionEngine.Enums;
using HoraLife.PredictionEngine.Models;
using KundaliEngine.API.DTOs.Responses;

namespace KundaliEngine.API.Prediction.Mappers;

public class PredictionContextMapper
{
    public PredictionContext Map(GenerateKundaliResponse source)
    {
        ArgumentNullException.ThrowIfNull(source);

        return new PredictionContext
        {
            Houses = MapHouses(source.Houses),
            Planets = MapPlanets(source.Planets),
            DashaTimeline = MapDashaTimeline(source.MahadashaPeriods),
            DivisionalCharts = MapDivisionalCharts(source),
            Yogas = MapYogas(source.Yogas),
            Doshas = MapDoshas(source.Doshas)
        };
    }

    private static IReadOnlyList<HousePredictionData> MapHouses(IReadOnlyList<HouseDto>? houses)
    {
        if (houses is null || houses.Count == 0)
        {
            return Array.Empty<HousePredictionData>();
        }

        return houses
            .Select(house => new HousePredictionData
            {
                HouseNumber = house.HouseNumber,
                Sign = house.Sign,
                HouseLord = house.HouseOwner,
                Occupants = house.Occupants?.ToArray()
                    ?? Array.Empty<string>(),
                AspectedBy = house.AspectedBy?.ToArray()
                    ?? Array.Empty<string>(),
                HasBeneficAspect = house.HasBeneficAspect,
                HasMaleficAspect = house.HasMaleficAspect,
                StrengthScore = house.Score,
                DominantThemes = house.DominantThemes?.ToArray()
                    ?? Array.Empty<string>()
            })
            .ToArray();
    }

    private static IReadOnlyList<PlanetPredictionData> MapPlanets(IReadOnlyList<PlanetDto>? planets)
    {
        if (planets is null || planets.Count == 0)
        {
            return Array.Empty<PlanetPredictionData>();
        }

        return planets
            .Select(planet => new PlanetPredictionData
            {
                Planet = planet.Planet,
                Sign = planet.Sign,
                HouseNumber = planet.House,
                Longitude = planet.Longitude,
                Degree = planet.Degree,
                Minute = planet.Minute,
                Nakshatra = planet.Nakshatra,
                Pada = planet.Pada,
                IsCombust = planet.IsCombust,
                IsRetrograde = planet.IsRetrograde,
                AspectedBy = planet.AspectedBy?.ToArray()
                    ?? Array.Empty<string>(),
                HasBeneficAspect = planet.HasBeneficAspect,
                HasMaleficAspect = planet.HasMaleficAspect,
                DetailedStrength = planet.DetailedStrength,
                ConditionTone = planet.ConditionTone
            })
            .ToArray();
    }

    private static IReadOnlyList<DivisionalChartPredictionData> MapDivisionalCharts(
        GenerateKundaliResponse source)
    {
        var charts = new List<DivisionalChartPredictionData>();

        if (source.D9Chart is not null)
        {
            charts.Add(MapDivisionalChart(source.D9Chart));
        }

        return charts;
    }

    private static DivisionalChartPredictionData MapDivisionalChart(
        DivisionalChartDto source)
    {
        return new DivisionalChartPredictionData
        {
            ChartType = source.ChartType,
            AscendantSign = ParseAscendantSign(source.Ascendant),
            Houses = source.Houses?.Select(house => new DivisionalHousePredictionData
            {
                HouseNumber = house.HouseNumber,
                Sign = house.Sign,
                HouseLord = house.HouseOwner,
                Occupants = house.Occupants?.ToArray()
                    ?? Array.Empty<string>()
            }).ToArray() ?? Array.Empty<DivisionalHousePredictionData>(),
            Planets = source.Planets?.Select(planet => new DivisionalPlanetPredictionData
            {
                Planet = planet.Planet,
                Sign = planet.Sign,
                HouseNumber = planet.House
            }).ToArray() ?? Array.Empty<DivisionalPlanetPredictionData>()
        };
    }

    private static string ParseAscendantSign(string? ascendant)
    {
        if (string.IsNullOrWhiteSpace(ascendant))
        {
            return string.Empty;
        }

        var parts = ascendant.Split('-', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        return parts.Length == 0 ? ascendant.Trim() : parts[^1];
    }

    private static DashaTimeline MapDashaTimeline(IReadOnlyList<MahadashaPeriodDto>? mahadashas)
    {
        if (mahadashas is null || mahadashas.Count == 0)
        {
            return new DashaTimeline();
        }

        var periods = new List<DashaPeriod>();

        foreach (var maha in mahadashas)
        {
            var mahaId = $"MD:{maha.Planet}:{maha.Start:O}";

            periods.Add(new DashaPeriod
            {
                PeriodId = mahaId,
                ParentPeriodId = null,
                Level = DashaLevel.Mahadasha,
                Planet = maha.Planet,
                StartDate = new DateTimeOffset(maha.Start),
                EndDate = new DateTimeOffset(maha.End)
            });

            foreach (var antar in maha.Antardashas)
            {
                var antarId = $"AD:{maha.Planet}:{antar.SubPlanet}:{antar.StartDate:O}";

                periods.Add(new DashaPeriod
                {
                    PeriodId = antarId,
                    ParentPeriodId = mahaId,
                    Level = DashaLevel.Antardasha,
                    Planet = antar.SubPlanet,
                    StartDate = new DateTimeOffset(antar.StartDate),
                    EndDate = new DateTimeOffset(antar.EndDate)
                });

                foreach (var pratyantar in antar.Pratyantardashas)
                {
                    var pratyantarId = $"PD:{maha.Planet}:{antar.SubPlanet}:{pratyantar.InnerPlanet}:{pratyantar.StartDate:O}";

                    periods.Add(new DashaPeriod
                    {
                        PeriodId = pratyantarId,
                        ParentPeriodId = antarId,
                        Level = DashaLevel.Pratyantardasha,
                        Planet = pratyantar.InnerPlanet,
                        StartDate = new DateTimeOffset(pratyantar.StartDate),
                        EndDate = new DateTimeOffset(pratyantar.EndDate)
                    });
                }
            }
        }

        return new DashaTimeline
        {
            Periods = periods
        };
    }

    private static IReadOnlyList<YogaPredictionData> MapYogas(IReadOnlyList<YogaDto>? yogas)
    {
        if (yogas is null || yogas.Count == 0)
        {
            return Array.Empty<YogaPredictionData>();
        }

        return yogas
            .Select(yoga => new YogaPredictionData
            {
                Key = yoga.Key,
                Name = yoga.Name,
                Category = yoga.Category,
                Strength = yoga.Strength,
                InvolvedPlanets = yoga.InvolvedPlanets?.ToArray()
                    ?? Array.Empty<string>(),
                InvolvedHouses = yoga.InvolvedHouses?.ToArray()
                    ?? Array.Empty<int>(),
                Reason = yoga.Reason
            })
            .ToArray();
    }

    private static IReadOnlyList<DoshaPredictionData> MapDoshas(IReadOnlyList<DoshaDto>? doshas)
    {
        if (doshas is null || doshas.Count == 0)
        {
            return Array.Empty<DoshaPredictionData>();
        }

        return doshas
            .Where(dosha => dosha.IsPresent)
            .Select(dosha => new DoshaPredictionData
            {
                Key = dosha.Key,
                Name = dosha.Name,
                Category = dosha.Category,
                Severity = dosha.Severity,
                InvolvedPlanets = dosha.InvolvedPlanets?.ToArray()
                    ?? Array.Empty<string>(),
                InvolvedHouses = dosha.InvolvedHouses?.ToArray()
                    ?? Array.Empty<int>(),
                Reason = dosha.Reason
            })
            .ToArray();
    }
}
