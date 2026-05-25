using KundaliEngine.API.DTOs.Requests;
using KundaliEngine.API.DTOs.Responses;
using KundaliEngine.Astrology.Astronomy;
using KundaliEngine.Astrology.Services.Astrology;
using KundaliEngine.Astrology.Services.Dasha;
using KundaliEngine.Astrology.Services.House;
using KundaliEngine.Astrology.Services.Panchang;
using KundaliEngine.Astrology.Services.Scoring;
using KundaliEngine.Domain.Enums;
using KundaliEngine.Domain.Models;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;

namespace KundaliEngine.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class KundaliController : ControllerBase
    {
        private readonly ChartService _chartService;
        private readonly PanchangService _panchangService;
        private readonly PlanetScoreService _planetScoreService;
        private readonly HouseScoreService _houseScoreservice;
        private readonly DashaService _dashaService;
        private readonly OverallKundaliScoreService _overallScoreService;
        private readonly AspectService _aspectService;

        public KundaliController(ChartService chartService, PanchangService panchangService, PlanetScoreService planetScoreService, HouseScoreService houseScoreService, DashaService dashaService, OverallKundaliScoreService overallScoreService, AspectService aspectService)
        {
            _chartService = chartService;
            _panchangService = panchangService;
            _planetScoreService = planetScoreService;
            _houseScoreservice = houseScoreService;
            _dashaService = dashaService;
            _overallScoreService = overallScoreService;
            _aspectService = aspectService;
        }

        [HttpPost("generate")]
        public ActionResult<GenerateKundaliResponse> Generate([FromBody] GenerateKundaliRequest request)
        {
            if (request == null)
                return BadRequest("Request is required");

            // Generate planet positions (Kundali chart)
            TimeZoneInfo indiaZone = TimeZoneInfo.FindSystemTimeZoneById("India Standard Time");

            DateTime birthUtc = TimeZoneInfo.ConvertTimeToUtc(request.BirthDateTime, indiaZone);

            request.BirthDateTime= birthUtc;
            var planets = _chartService.GenerateChart(request.BirthDateTime, request.Latitude, request.Longitude );
            // Panchang
            double sunLongitude = planets.First(p => p.Planet == Planet.Sun).Longitude;
            double moonLongitude = planets.First(p => p.Planet == Planet.Moon).Longitude;
            var panchang = _panchangService.Calculate(request.BirthDateTime, sunLongitude, moonLongitude);

            // Dasha
            var moonPos = planets.First(p => p.Planet == Planet.Moon);
            var birthDasha = _dashaService.GetBirthMahadasha(moonPos);

            // Ascendant
            var ascCalc = new AscendantCalculator();
            double ascendantLongitude = ascCalc.Calculate(request.BirthDateTime, request.Latitude, request.Longitude);
            int ascendantIndex = (int)(ascendantLongitude / 30.0);
            ZodiacSign ascendantSign = (ZodiacSign)((ascendantIndex % 12) + 1);

            // Houses and house scores
            var aspectResult = _aspectService.CalculateGrahaDrishti(planets);
            var houseScores = _houseScoreservice.CalculateAllHouseScores(ascendantSign, planets, aspectResult);

            // Compute house signs (house N sign = ascendantSign + (N-1))
            var houseDtos = new List<HouseDto>();
            for (int i = 1; i <= 12; i++)
            {
                var houseNumber = (HouseNumber)i;
                var signIndex = (((int)ascendantSign - 1) + (i - 1)) % 12;
                var houseSign = (ZodiacSign)(signIndex + 1);
                var houseOwner = GetSignOwner(houseSign);

                var houseAspects = aspectResult.HouseAspects
                    .Where(a => a.ToHouse == i)
                    .ToList();
                var occupants = planets.Where(p => (int)p.House == i).Select(p => p.Planet.ToString()).ToList();
                var scoreResult = houseScores.First(h => (int)h.HouseNumber == i);


                houseDtos.Add(new HouseDto
                {
                    HouseNumber = i,
                    Sign = houseSign.ToString(),
                    HouseOwner = houseOwner,
                    HouseSwamy = houseOwner,
                    Occupants = occupants,
                    HasBeneficAspect = houseAspects.Any(a => IsNaturalBenefic(a.FromPlanet)),
                    HasMaleficAspect = houseAspects.Any(a => IsNaturalMaleficOrShadow(a.FromPlanet)),
                    Score = scoreResult.Score,
                    Factors = scoreResult.Factors,
                    DominantThemes = scoreResult.DominantThemes,
                    HouseName = HouseMetadataService.GetHouseName(i),
                    LifeAreas = HouseMetadataService.GetLifeAreas(i),
                });
            }

            // Overall score
            var overall = _overallScoreService.Calculate(ascendantSign, planets, aspectResult, null);
            var overallDto = new OverallScoreDto
            {
                Overall = overall.OverallScore,
                Stability = overall.StabilityScore,
                Growth = overall.GrowthScore,
                Challenge = overall.ChallengeScore,
                DominantStrengths = overall.DominantStrengths,
                DominantWeaknesses = overall.DominantWeaknesses
            };

            // Current Dasha
            var mahadashaTimeline = _dashaService.GenerateMahadashaTimeline(birthDasha.Planet, birthDasha.RemainingYears, request.BirthDateTime);
            var currentMaha = mahadashaTimeline.FirstOrDefault(m => DateTime.UtcNow >= m.StartDate && DateTime.UtcNow < m.EndDate) ?? mahadashaTimeline.First();
            var currentAntarList = _dashaService.GenerateAntardasha(currentMaha, _dashaService.GetBirthMahadasha(moonPos).RemainingYears);
            var currentAntar = currentAntarList.FirstOrDefault(a => DateTime.UtcNow >= a.StartDate && DateTime.UtcNow < a.EndDate) ?? currentAntarList.First();

            var currentDashaDto = new CurrentDashaDto
            {
                Mahadasha = currentMaha.Planet.ToString(),
                Antardasha = currentAntar.SubPlanet.ToString(),
                StartDate = currentMaha.StartDate,
                EndDate = currentMaha.EndDate,
                Summary = ""
            };

            // Build response (mapped)
            var response = new GenerateKundaliResponse
            {
                Summary = new KundaliSummaryDto
                {
                    Name = request.Name,
                    Ascendant = $"{(int)ascendantSign} - {ascendantSign}",
                    MoonSign = moonPos.Sign.ToString(),
                    SunSign = planets.First(p => p.Planet == Planet.Sun).Sign.ToString(),
                    Nakshatra = panchang.Nakshatra,
                    Tithi = panchang.Tithi,
                    CurrentMahadasha = birthDasha.Planet.ToString(),
                    CurrentAntardasha = currentDashaDto.Antardasha,
                    Summary = "Kundali generated successfully"
                },
                Panchang = new PanchangDto
                {
                    Tithi = panchang.Tithi,
                    Paksha = panchang.Paksha,
                    Nakshatra = panchang.Nakshatra,
                    Pada = panchang.Pada,
                    Yoga = panchang.Yoga,
                    Karana = panchang.Karana,
                    Vaar = panchang.Vaar,
                    Maas = panchang.Maas
                },
                Planets = planets.Select(p => new PlanetDto
                {
                    Planet = p.Planet.ToString(),
                    Sign = p.Sign.ToString(),
                    Degree = p.Degree,
                    Minute = p.Minute,
                    House = (int)p.House,
                    Longitude = p.Longitude,
                    Latitude = p.Latitude,
                    IsCombust = p.IsCombust,
                    IsRetrograde = p.IsRetrograde,
                    DetailedStrength = p.DetailedStrength,
                    Nakshatra = p.Nakshatra.ToString(),
                    Pada = p.Pada
                }).ToList(),
                Houses = houseDtos,
                Scores = overallDto,
                CurrentDasha = currentDashaDto
            };

            return Ok(response);
        }

        [HttpPost("planet-positions")]
        public ActionResult<List<PlanetDto>> PlanetPositions([FromBody] GenerateKundaliRequest request)
        {
            if (request == null)
                return BadRequest("Request is required");

            var planets = _chartService.GenerateChart(request.BirthDateTime, request.Latitude, request.Longitude);

            var result = planets.Select(p => new PlanetDto
            {
                Planet = p.Planet.ToString(),
                Sign = p.Sign.ToString(),
                Degree = p.Degree,
                Minute = p.Minute,
                House = (int)p.House,
                Longitude = p.Longitude,
                Latitude = p.Latitude,
                IsCombust = p.IsCombust,
                IsRetrograde = p.IsRetrograde,
                DetailedStrength = p.DetailedStrength,
                Nakshatra = p.Nakshatra.ToString(),
                Pada = p.Pada
            }).ToList();

            return Ok(result);
        }

        [HttpPost("houses")]
        public ActionResult<List<HouseDto>> Houses([FromBody] GenerateKundaliRequest request)
        {
            if (request == null)
                return BadRequest("Request is required");

            var planets = _chartService.GenerateChart(request.BirthDateTime, request.Latitude, request.Longitude);

            // Placeholder: return empty house DTOs for Phase 1
            var result = Enumerable.Range(1, 12).Select(i => new HouseDto()).ToList();

            return Ok(result);
        }

        [HttpPost("summary")]
        public ActionResult<KundaliSummaryDto> Summary([FromBody] GenerateKundaliRequest request)
        {
            if (request == null)
                return BadRequest("Request is required");

            var planets = _chartService.GenerateChart(request.BirthDateTime, request.Latitude, request.Longitude);

            var dto = new KundaliSummaryDto();

            return Ok(dto);
        }

        [HttpPost("score")]
        public ActionResult<OverallScoreDto> Score([FromBody] GenerateKundaliRequest request)
        {
            if (request == null)
                return BadRequest("Request is required");

            var planets = _chartService.GenerateChart(request.BirthDateTime, request.Latitude, request.Longitude);

            // Phase 1: lightweight aggregate using planet base scores
            var planetScores = _planetScoreService.CalculateMany(planets);
            int avgPlanetScore = (int)Math.Round(planetScores.Average(p => p.Score));

            var overall = new OverallScoreDto();

            return Ok(overall);
        }

        private static string GetSignOwner(ZodiacSign sign)
        {
            return sign switch
            {
                ZodiacSign.Aries => Planet.Mars.ToString(),
                ZodiacSign.Taurus => Planet.Venus.ToString(),
                ZodiacSign.Gemini => Planet.Mercury.ToString(),
                ZodiacSign.Cancer => Planet.Moon.ToString(),
                ZodiacSign.Leo => Planet.Sun.ToString(),
                ZodiacSign.Virgo => Planet.Mercury.ToString(),
                ZodiacSign.Libra => Planet.Venus.ToString(),
                ZodiacSign.Scorpio => Planet.Mars.ToString(),
                ZodiacSign.Sagittarius => Planet.Jupiter.ToString(),
                ZodiacSign.Capricorn => Planet.Saturn.ToString(),
                ZodiacSign.Aquarius => Planet.Saturn.ToString(),
                ZodiacSign.Pisces => Planet.Jupiter.ToString(),
                _ => string.Empty
            };
        }

        private static bool IsNaturalBenefic(Planet planet)
        {
            return planet is Planet.Moon or Planet.Venus or Planet.Jupiter;
        }

        private static bool IsNaturalMaleficOrShadow(Planet planet)
        {
            return planet is Planet.Sun or Planet.Mars or Planet.Saturn or Planet.Rahu or Planet.Ketu;
        }
    }
}
