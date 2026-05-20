using KundaliEngine.API.DTOs.Requests;
using KundaliEngine.API.DTOs.Responses;
using KundaliEngine.Astrology.Astronomy;
using KundaliEngine.Astrology.Services.Astrology;
using KundaliEngine.Astrology.Services.Dasha;
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

        public KundaliController(ChartService chartService, PanchangService panchangService, PlanetScoreService planetScoreService, HouseScoreService houseScoreService, DashaService dashaService, OverallKundaliScoreService overallScoreService)
        {
            _chartService = chartService;
            _panchangService = panchangService;
            _planetScoreService = planetScoreService;
            _houseScoreservice = houseScoreService;
            _dashaService = dashaService;
            _overallScoreService = overallScoreService;
        }

        [HttpPost("generate")]
        public ActionResult<GenerateKundaliResponse> Generate([FromBody] GenerateKundaliRequest request)
        {
            if (request == null)
                return BadRequest("Request is required");

            // Generate planet positions (Kundali chart)
            var planets = _chartService.GenerateChart(request.BirthDateTime, request.Latitude, request.Longitude);

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
            var houseScores = _houseScoreservice.CalculateAllHouseScores(ascendantSign, planets, null);

            // Compute house signs (house N sign = ascendantSign + (N-1))
            var houseDtos = new List<HouseDto>();
            for (int i = 1; i <= 12; i++)
            {
                var houseNumber = (HouseNumber)i;
                var signIndex = (((int)ascendantSign - 1) + (i - 1)) % 12;
                var houseSign = (ZodiacSign)(signIndex + 1);

                var occupants = planets.Where(p => (int)p.House == i).Select(p => p.Planet.ToString()).ToList();
                var scoreResult = houseScores.First(h => (int)h.HouseNumber == i);

                houseDtos.Add(new HouseDto
                {
                    HouseNumber = i,
                    Sign = houseSign.ToString(),
                    Occupants = occupants,
                    Score = scoreResult.Score,
                    Factors = scoreResult.Factors,
                    DominantThemes = scoreResult.DominantThemes
                });
            }

            // Overall score
            var overall = _overallScoreService.Calculate(ascendantSign, planets, null, null);
            var overallDto = new OverallScoreDto
            {
                Overall = overall.OverallScore,
                Stability = overall.StabilityScore,
                Growth = overall.GrowthScore,
                Challenge = overall.ChallengeScore,
                DominantStrengths = overall.DominantStrengths,
                DominantWeaknesses = overall.DominantWeaknesses
            };

            // Dasha timeline for 120 years (full Vimshottari cycle)
            var mahadashaTimeline = _dashaService.GenerateMahadashaTimeline(birthDasha.Planet, birthDasha.RemainingYears, request.BirthDateTime);
            // Ensure timeline covers at least 120 years by extending cycles if needed
            while ((mahadashaTimeline.Last().EndDate - mahadashaTimeline.First().StartDate).TotalDays < 120 * 365.25)
            {
                var lastPlanet = mahadashaTimeline.Last().Planet;
                var extend = _dashaService.GenerateMahadashaTimeline(lastPlanet, _dashaService.GetBirthMahadasha(moonPos).RemainingYears, mahadashaTimeline.Last().EndDate);
                mahadashaTimeline.AddRange(extend);
            }

            // Build mahadasha + antardasha DTOs
            var mahadashaDtos = mahadashaTimeline.Select(m => new MahadashaPeriodDto
            {
                Planet = m.Planet.ToString(),
                Start = m.StartDate,
                End = m.EndDate,
                Antardashas = _dashaService.GenerateAntardasha(m, _dashaService.GetBirthMahadasha(moonPos).RemainingYears)
                    .Select(a => new AntardashaPeriodDto { SubPlanet = a.SubPlanet.ToString(), StartDate = a.StartDate, EndDate = a.EndDate })
                    .ToList()
            }).ToList();

            // Current Dasha
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
                CurrentDasha = currentDashaDto,
                MahadashaPeriods = mahadashaDtos // Expose full mahadasha timeline
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
    }
}
