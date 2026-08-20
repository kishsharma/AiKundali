using KundaliEngine.API.DTOs.Responses;
using KundaliEngine.Astrology.Astronomy;
using KundaliEngine.Astrology.Services.Panchang;
using KundaliEngine.API.Services.Muhurtha;
using KundaliEngine.API.Services.Panchang;
using KundaliEngine.Domain.Models.Muhurtha;
using KundaliEngine.Domain.Enums;
using KundaliEngine.Domain.Models;
using KundaliEngine.KnowledgeBase.Services;
using Microsoft.AspNetCore.Mvc;

namespace KundaliEngine.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PanchangController : ControllerBase
    {
        private const double DefaultLatitude = 12.9716;
        private const double DefaultLongitude = 77.5946;
        private const string DefaultTimezone = "Asia/Kolkata";

        private readonly PanchangService _panchangService;
        private readonly IEphemerisProvider _ephemerisProvider;
        private readonly IMuhurthaSunriseSunsetService _sunriseSunsetService;
        private readonly NakshatraMetadataService _nakshatraMetadataService;
        private readonly RashiMetadataService _rashiMetadataService;
        private readonly IPanchangEclipseService _panchangEclipseService;

        public PanchangController(
            PanchangService panchangService,
            IEphemerisProvider ephemerisProvider,
            IMuhurthaSunriseSunsetService sunriseSunsetService,
            NakshatraMetadataService nakshatraMetadataService,
            RashiMetadataService rashiMetadataService,
            IPanchangEclipseService panchangEclipseService)
        {
            _panchangService = panchangService;
            _ephemerisProvider = ephemerisProvider;
            _sunriseSunsetService = sunriseSunsetService;
            _nakshatraMetadataService = nakshatraMetadataService;
            _rashiMetadataService = rashiMetadataService;
            _panchangEclipseService = panchangEclipseService;
        }

        [HttpGet("calendar")]
        public ActionResult<PanchangApiResponse<PanchangCalendarResponseDto>> GetCalendar(
            [FromQuery] string mode = "monthly",
            [FromQuery] DateOnly? date = null,
            [FromQuery] double latitude = DefaultLatitude,
            [FromQuery] double longitude = DefaultLongitude,
            [FromQuery] string timezone = DefaultTimezone)
        {
            if (!TryNormalizeMode(mode, out var normalizedMode))
            {
                return BadRequest(new PanchangApiResponse<PanchangCalendarResponseDto>
                {
                    Success = false,
                    Message = "Mode must be daily, weekly, or monthly."
                });
            }

            if (!IsValidLocation(latitude, longitude))
            {
                return BadRequest(new PanchangApiResponse<PanchangCalendarResponseDto>
                {
                    Success = false,
                    Message = "Latitude must be between -90 and 90, and longitude must be between -180 and 180."
                });
            }

            var anchorDate = date ?? DateOnly.FromDateTime(DateTime.Today);
            var (startDate, endDate) = GetDateRange(normalizedMode, anchorDate);
            var days = BuildPanchangDays(startDate, endDate, latitude, longitude, timezone);

            return Ok(new PanchangApiResponse<PanchangCalendarResponseDto>
            {
                Success = true,
                Data = new PanchangCalendarResponseDto
                {
                    Mode = normalizedMode,
                    StartDate = startDate,
                    EndDate = endDate,
                    Latitude = latitude,
                    Longitude = longitude,
                    Timezone = timezone,
                    Days = days
                }
            });
        }

        [HttpGet("{date}")]
        public ActionResult<PanchangApiResponse<PanchangCalendarDayDto>> GetDaily(
            DateOnly date,
            [FromQuery] double latitude = DefaultLatitude,
            [FromQuery] double longitude = DefaultLongitude,
            [FromQuery] string timezone = DefaultTimezone)
        {
            if (!IsValidLocation(latitude, longitude))
            {
                return BadRequest(new PanchangApiResponse<PanchangCalendarDayDto>
                {
                    Success = false,
                    Message = "Latitude must be between -90 and 90, and longitude must be between -180 and 180."
                });
            }

            var day = BuildPanchangDay(date, latitude, longitude, timezone);
            return Ok(new PanchangApiResponse<PanchangCalendarDayDto>
            {
                Success = true,
                Data = day
            });
        }

        [HttpGet("lagna-timings/{date}")]
        public ActionResult<PanchangApiResponse<PanchangLagnaTimingsResponseDto>> GetLagnaTimings(
            DateOnly date,
            [FromQuery] double latitude = DefaultLatitude,
            [FromQuery] double longitude = DefaultLongitude,
            [FromQuery] string timezone = DefaultTimezone)
        {
            if (!IsValidLocation(latitude, longitude))
            {
                return BadRequest(new PanchangApiResponse<PanchangLagnaTimingsResponseDto>
                {
                    Success = false,
                    Message = "Latitude must be between -90 and 90, and longitude must be between -180 and 180."
                });
            }

            var location = new GeoLocation(latitude, longitude);
            var localDateTime = date.ToDateTime(new TimeOnly(6, 0));
            var offset = GetTimezoneOffset(localDateTime, timezone);
            var muhurthaLocation = new MuhurthaLocation
            {
                Latitude = latitude,
                Longitude = longitude,
                Timezone = timezone
            };
            var sunrise = _sunriseSunsetService.Calculate(date, muhurthaLocation, offset).Sunrise;
            var nextDate = date.AddDays(1);
            var nextOffset = GetTimezoneOffset(nextDate.ToDateTime(new TimeOnly(6, 0)), timezone);
            var nextSunrise = _sunriseSunsetService.Calculate(nextDate, muhurthaLocation, nextOffset).Sunrise;

            return Ok(new PanchangApiResponse<PanchangLagnaTimingsResponseDto>
            {
                Success = true,
                Data = new PanchangLagnaTimingsResponseDto
                {
                    Date = date,
                    Sunrise = sunrise,
                    NextSunrise = nextSunrise,
                    Timings = BuildLagnaTimings(sunrise, nextSunrise, location)
                }
            });
        }
        [HttpGet("range")]
        public ActionResult<PanchangApiResponse<List<PanchangCalendarDayDto>>> GetRange(
            [FromQuery] DateOnly startDate,
            [FromQuery] DateOnly endDate,
            [FromQuery] double latitude = DefaultLatitude,
            [FromQuery] double longitude = DefaultLongitude,
            [FromQuery] string timezone = DefaultTimezone)
        {
            if (endDate < startDate)
            {
                return BadRequest(new PanchangApiResponse<List<PanchangCalendarDayDto>>
                {
                    Success = false,
                    Message = "endDate must be on or after startDate."
                });
            }

            if (startDate.DayNumber + 62 < endDate.DayNumber)
            {
                return BadRequest(new PanchangApiResponse<List<PanchangCalendarDayDto>>
                {
                    Success = false,
                    Message = "Date range cannot exceed 63 days."
                });
            }

            if (!IsValidLocation(latitude, longitude))
            {
                return BadRequest(new PanchangApiResponse<List<PanchangCalendarDayDto>>
                {
                    Success = false,
                    Message = "Latitude must be between -90 and 90, and longitude must be between -180 and 180."
                });
            }

            return Ok(new PanchangApiResponse<List<PanchangCalendarDayDto>>
            {
                Success = true,
                Data = BuildPanchangDays(startDate, endDate, latitude, longitude, timezone)
            });
        }

        [HttpGet("eclipses")]
        public ActionResult<PanchangApiResponse<PanchangEclipseYearResponseDto>> GetYearlyEclipses(
            [FromQuery] int? year = null,
            [FromQuery] double latitude = DefaultLatitude,
            [FromQuery] double longitude = DefaultLongitude,
            [FromQuery] string timezone = DefaultTimezone)
        {
            if (!IsValidLocation(latitude, longitude))
            {
                return BadRequest(new PanchangApiResponse<PanchangEclipseYearResponseDto>
                {
                    Success = false,
                    Message = "Latitude must be between -90 and 90, and longitude must be between -180 and 180."
                });
            }

            var targetYear = year ?? DateTime.Today.Year;
            if (targetYear < 1900 || targetYear > 2100)
            {
                return BadRequest(new PanchangApiResponse<PanchangEclipseYearResponseDto>
                {
                    Success = false,
                    Message = "Year must be between 1900 and 2100."
                });
            }

            var eclipses = _panchangEclipseService.GetYearlyEclipses(targetYear, latitude, longitude, timezone);
            return Ok(new PanchangApiResponse<PanchangEclipseYearResponseDto>
            {
                Success = true,
                Data = new PanchangEclipseYearResponseDto
                {
                    Year = targetYear,
                    Latitude = latitude,
                    Longitude = longitude,
                    Timezone = timezone,
                    Eclipses = eclipses.ToList()
                }
            });
        }
        private List<PanchangCalendarDayDto> BuildPanchangDays(
            DateOnly startDate,
            DateOnly endDate,
            double latitude,
            double longitude,
            string timezone)
        {
            var days = new List<PanchangCalendarDayDto>();
            for (var date = startDate; date <= endDate; date = date.AddDays(1))
            {
                days.Add(BuildPanchangDay(date, latitude, longitude, timezone));
            }

            return days;
        }

        private PanchangCalendarDayDto BuildPanchangDay(
            DateOnly date,
            double latitude,
            double longitude,
            string timezone)
        {
            var calculationLocal = date.ToDateTime(new TimeOnly(6, 0));
            var calculationUtc = ConvertLocalToUtc(calculationLocal, timezone);
            var location = new GeoLocation(latitude, longitude);
            var sun = _ephemerisProvider.GetPlanet(Planet.Sun, calculationUtc, location);
            var moon = _ephemerisProvider.GetPlanet(Planet.Moon, calculationUtc, location);
            var details = _panchangService.Calculate(calculationLocal, sun.Longitude, moon.Longitude);
            var offset = GetTimezoneOffset(calculationLocal, timezone);
            var solarEvents = _sunriseSunsetService.Calculate(date, new MuhurthaLocation
            {
                Latitude = latitude,
                Longitude = longitude,
                Timezone = timezone
            }, offset);
            var midpoint = new DateTimeOffset(calculationLocal, offset);
            var panchangIntervals = BuildPanchangIntervals(midpoint, location);
            var nakshatraPadas = BuildNakshatraPadas(details.Nakshatra, details.Pada, midpoint, panchangIntervals["nakshatra"], location);
            var lunarEvents = CalculateMoonRiseSet(date, location, offset);


            return new PanchangCalendarDayDto
            {
                Id = date.ToString("yyyy-MM-dd"),
                Date = date,
                Tithi = details.Tithi,
                TithiEndTime = FormatTime(panchangIntervals["tithi"].End),
                Paksha = details.Paksha,
                Nakshatra = details.Nakshatra,
                NakshatraEndTime = FormatTime(panchangIntervals["nakshatra"].End),
                Pada = details.Pada,
                Akshara = details.Akshara,
                NakshatraPadas = nakshatraPadas,
                Yoga = details.Yoga,
                YogaEndTime = FormatTime(panchangIntervals["yoga"].End),
                Karana = details.Karana,
                KaranaEndTime = FormatTime(panchangIntervals["karana"].End),
                Vaar = details.Vaar,
                DayOfWeek = details.Vaar,
                Maas = details.Maas,
                Shake = details.Shake,
                Samvat = details.Samvat,
                SunRiseTime = FormatTime(solarEvents.Sunrise),
                SunSetTime = FormatTime(solarEvents.Sunset),
                MoonRiseTime = FormatOptionalTime(lunarEvents.Moonrise),
                MoonSetTime = FormatOptionalTime(lunarEvents.Moonset),
                IsAuspicious = false,
                Summary = BuildSummary(details),
                AuspiciousTimings = new List<AuspiciousTimingDto>()
            };
        }

        private List<LagnaTimingDto> BuildLagnaTimings(
            DateTimeOffset sunrise,
            DateTimeOffset nextSunrise,
            GeoLocation location)
        {
            var timings = new List<LagnaTimingDto>();
            var periodStart = sunrise;
            var sign = AscendantSign(periodStart.UtcDateTime, location);

            while (periodStart < nextSunrise)
            {
                var transition = FindNextLagnaTransition(periodStart, nextSunrise, sign, location);
                var metadata = _rashiMetadataService.GetRashiBySign(sign.ToString());
                timings.Add(new LagnaTimingDto
                {
                    Sign = metadata?.Name ?? sign.ToString(),
                    Lord = metadata?.Ruler ?? string.Empty,
                    EndTime = transition
                });

                if (transition >= nextSunrise)
                {
                    break;
                }

                periodStart = transition.AddSeconds(1);
                sign = AscendantSign(periodStart.UtcDateTime, location);
            }

            return timings;
        }

        private DateTimeOffset FindNextLagnaTransition(
            DateTimeOffset start,
            DateTimeOffset limit,
            ZodiacSign sign,
            GeoLocation location)
        {
            var inside = start;
            var outside = start.AddMinutes(10);

            while (outside < limit && AscendantSign(outside.UtcDateTime, location) == sign)
            {
                inside = outside;
                outside = outside.AddMinutes(10);
            }

            if (outside >= limit)
            {
                outside = limit;
                if (AscendantSign(outside.UtcDateTime, location) == sign)
                {
                    return limit;
                }
            }

            for (var i = 0; i < 24; i++)
            {
                var probe = new DateTimeOffset(inside.Ticks + ((outside.Ticks - inside.Ticks) / 2), inside.Offset);
                if (AscendantSign(probe.UtcDateTime, location) == sign)
                {
                    inside = probe;
                }
                else
                {
                    outside = probe;
                }
            }

            return outside;
        }

        private ZodiacSign AscendantSign(DateTime utc, GeoLocation location)
        {
            var longitude = _ephemerisProvider.GetAscendant(utc, location).Longitude;
            return (ZodiacSign)(Math.Floor(NormalizeLongitude(longitude) / 30.0) + 1);
        }
        private List<NakshatraPadaTimingDto> BuildNakshatraPadas(
            string nakshatraName,
            int currentPada,
            DateTimeOffset midpoint,
            (DateTimeOffset Start, DateTimeOffset End) nakshatraInterval,
            GeoLocation location)
        {
            var padas = new List<NakshatraPadaTimingDto>();
            var nakshatraIndex = NakshatraIndex(midpoint.UtcDateTime, location);
            var basePadaIndex = nakshatraIndex * 4;
            var durationTicks = nakshatraInterval.End.Ticks - nakshatraInterval.Start.Ticks;

            for (var pada = 1; pada <= 4; pada++)
            {
                var padaIndex = basePadaIndex + pada - 1;
                var approximateTicks = nakshatraInterval.Start.Ticks + (durationTicks * ((pada * 2) - 1) / 8);
                var approximateMidpoint = new DateTimeOffset(approximateTicks, nakshatraInterval.Start.Offset);
                var insidePada = EnsureInsidePada(approximateMidpoint, padaIndex, nakshatraInterval, location);
                var padaInterval = BuildInterval(
                    insidePada,
                    time => NakshatraPadaIndex(time, location),
                    TimeSpan.FromMinutes(45),
                    TimeSpan.FromHours(12));

                padas.Add(new NakshatraPadaTimingDto
                {
                    Pada = pada,
                    Syllable = _nakshatraMetadataService.GetPadaSyllable(nakshatraName, pada),
                    StartTime = FormatTime(padaInterval.Start),
                    EndTime = FormatTime(padaInterval.End),
                    IsCurrent = pada == currentPada
                });
            }

            return padas;
        }

        private DateTimeOffset EnsureInsidePada(
            DateTimeOffset candidate,
            int padaIndex,
            (DateTimeOffset Start, DateTimeOffset End) nakshatraInterval,
            GeoLocation location)
        {
            if (NakshatraPadaIndex(candidate.UtcDateTime, location) == padaIndex)
            {
                return candidate;
            }

            for (var probe = nakshatraInterval.Start.AddMinutes(5); probe < nakshatraInterval.End; probe = probe.AddMinutes(15))
            {
                if (NakshatraPadaIndex(probe.UtcDateTime, location) == padaIndex)
                {
                    return probe;
                }
            }

            return candidate;
        }

        private (DateTimeOffset? Moonrise, DateTimeOffset? Moonset) CalculateMoonRiseSet(
            DateOnly date,
            GeoLocation location,
            TimeSpan offset)
        {
            var dayStart = new DateTimeOffset(date.ToDateTime(TimeOnly.MinValue), offset);
            DateTimeOffset? moonrise = null;
            DateTimeOffset? moonset = null;
            var previousTime = dayStart;
            var previousAltitude = MoonAltitude(previousTime.UtcDateTime, location);

            for (var hour = 1; hour <= 24; hour++)
            {
                var currentTime = dayStart.AddHours(hour);
                var currentAltitude = MoonAltitude(currentTime.UtcDateTime, location);

                if (!IsFinite(previousAltitude))
                {
                    previousTime = currentTime;
                    previousAltitude = currentAltitude;
                    continue;
                }

                if (!IsFinite(currentAltitude))
                {
                    continue;
                }

                if (previousAltitude <= 0 && currentAltitude > 0 && moonrise is null)
                {
                    moonrise = RefineAltitudeCrossing(previousTime, currentTime, location);
                }
                else if (previousAltitude >= 0 && currentAltitude < 0 && moonset is null)
                {
                    moonset = RefineAltitudeCrossing(previousTime, currentTime, location);
                }

                if (moonrise is not null && moonset is not null)
                {
                    break;
                }

                previousTime = currentTime;
                previousAltitude = currentAltitude;
            }

            return (moonrise, moonset);
        }

        private DateTimeOffset? RefineAltitudeCrossing(
            DateTimeOffset start,
            DateTimeOffset end,
            GeoLocation location)
        {
            var startAltitude = MoonAltitude(start.UtcDateTime, location);
            if (!IsFinite(startAltitude))
            {
                return null;
            }

            for (var i = 0; i < 24; i++)
            {
                var midpoint = new DateTimeOffset(start.Ticks + ((end.Ticks - start.Ticks) / 2), start.Offset);
                var midpointAltitude = MoonAltitude(midpoint.UtcDateTime, location);
                if (!IsFinite(midpointAltitude))
                {
                    end = midpoint;
                    continue;
                }

                if (Math.Sign(midpointAltitude) == Math.Sign(startAltitude))
                {
                    start = midpoint;
                    startAltitude = midpointAltitude;
                }
                else
                {
                    end = midpoint;
                }
            }

            return end;
        }

        private double MoonAltitude(DateTime utc, GeoLocation location)
        {
            var moon = _ephemerisProvider.GetPlanet(Planet.Moon, utc, location);
            if (!IsFinite(moon.Longitude) || !IsFinite(moon.Latitude))
            {
                return double.NaN;
            }

            var (rightAscension, declination) = EclipticToEquatorial(moon.Longitude, moon.Latitude);
            if (!IsFinite(rightAscension) || !IsFinite(declination))
            {
                return double.NaN;
            }

            var localSiderealTime = NormalizeLongitude(GreenwichSiderealTime(utc) + location.Longitude);
            var hourAngle = DegreesToRadians(NormalizeSignedAngle(localSiderealTime - rightAscension));
            var latitude = DegreesToRadians(location.Latitude);
            var declinationRadians = DegreesToRadians(declination);
            var altitudeArgument = Math.Sin(latitude) * Math.Sin(declinationRadians)
                + Math.Cos(latitude) * Math.Cos(declinationRadians) * Math.Cos(hourAngle);

            if (!IsFinite(altitudeArgument))
            {
                return double.NaN;
            }

            var altitude = Math.Asin(ClampUnit(altitudeArgument));
            return RadiansToDegrees(altitude) + 0.3;
        }

        private static (double RightAscension, double Declination) EclipticToEquatorial(double longitude, double latitude)
        {
            if (!IsFinite(longitude) || !IsFinite(latitude))
            {
                return (double.NaN, double.NaN);
            }

            var obliquity = DegreesToRadians(23.439291);
            var lambda = DegreesToRadians(longitude);
            var beta = DegreesToRadians(latitude);
            var rightAscension = Math.Atan2(
                Math.Sin(lambda) * Math.Cos(obliquity) - Math.Tan(beta) * Math.Sin(obliquity),
                Math.Cos(lambda));
            var declinationArgument = Math.Sin(beta) * Math.Cos(obliquity)
                + Math.Cos(beta) * Math.Sin(obliquity) * Math.Sin(lambda);
            var declination = Math.Asin(ClampUnit(declinationArgument));

            return (NormalizeLongitude(RadiansToDegrees(rightAscension)), RadiansToDegrees(declination));
        }


        private static bool IsFinite(double value)
        {
            return !double.IsNaN(value) && !double.IsInfinity(value);
        }

        private static double ClampUnit(double value)
        {
            return Math.Clamp(value, -1.0, 1.0);
        }
        private static double GreenwichSiderealTime(DateTime utc)
        {
            var julianDay = ToJulianDay(utc);
            var centuries = (julianDay - 2451545.0) / 36525.0;
            return NormalizeLongitude(
                280.46061837
                + 360.98564736629 * (julianDay - 2451545.0)
                + 0.000387933 * centuries * centuries
                - (centuries * centuries * centuries) / 38710000.0);
        }

        private static double ToJulianDay(DateTime utc)
        {
            var year = utc.Year;
            var month = utc.Month;
            var day = utc.Day
                + (utc.Hour + (utc.Minute + (utc.Second + utc.Millisecond / 1000.0) / 60.0) / 60.0) / 24.0;

            if (month <= 2)
            {
                year -= 1;
                month += 12;
            }

            var a = year / 100;
            var b = 2 - a + (a / 4);
            return Math.Floor(365.25 * (year + 4716))
                + Math.Floor(30.6001 * (month + 1))
                + day + b - 1524.5;
        }

        private IReadOnlyDictionary<string, (DateTimeOffset Start, DateTimeOffset End)> BuildPanchangIntervals(
            DateTimeOffset midpoint,
            GeoLocation location)
        {
            return new Dictionary<string, (DateTimeOffset Start, DateTimeOffset End)>
            {
                ["tithi"] = BuildInterval(midpoint, time => TithiIndex(time, location), TimeSpan.FromHours(3), TimeSpan.FromHours(36)),
                ["nakshatra"] = BuildInterval(midpoint, time => NakshatraIndex(time, location), TimeSpan.FromHours(3), TimeSpan.FromHours(36)),
                ["yoga"] = BuildInterval(midpoint, time => YogaIndex(time, location), TimeSpan.FromHours(3), TimeSpan.FromHours(48)),
                ["karana"] = BuildInterval(midpoint, time => KaranaIndex(time, location), TimeSpan.FromMinutes(90), TimeSpan.FromHours(24))
            };
        }

        private (DateTimeOffset Start, DateTimeOffset End) BuildInterval(
            DateTimeOffset midpoint,
            Func<DateTime, int> keySelector,
            TimeSpan step,
            TimeSpan maxSearch)
        {
            var key = keySelector(midpoint.UtcDateTime);
            return (
                FindBoundary(midpoint, keySelector, key, -step, maxSearch, forward: false),
                FindBoundary(midpoint, keySelector, key, step, maxSearch, forward: true));
        }

        private static DateTimeOffset FindBoundary(
            DateTimeOffset midpoint,
            Func<DateTime, int> keySelector,
            int key,
            TimeSpan step,
            TimeSpan maxSearch,
            bool forward)
        {
            var inside = midpoint;
            var outside = midpoint + step;
            var searched = step.Duration();

            while (searched <= maxSearch && keySelector(outside.UtcDateTime) == key)
            {
                inside = outside;
                outside += step;
                searched += step.Duration();
            }

            if (keySelector(outside.UtcDateTime) == key)
            {
                return outside;
            }

            for (var i = 0; i < 24; i++)
            {
                var midpointTicks = inside.Ticks + ((outside.Ticks - inside.Ticks) / 2);
                var probe = new DateTimeOffset(midpointTicks, inside.Offset);
                if (keySelector(probe.UtcDateTime) == key)
                {
                    inside = probe;
                }
                else
                {
                    outside = probe;
                }
            }

            return forward ? outside : inside;
        }

        private int TithiIndex(DateTime utc, GeoLocation location)
        {
            var sun = _ephemerisProvider.GetPlanet(Planet.Sun, utc, location);
            var moon = _ephemerisProvider.GetPlanet(Planet.Moon, utc, location);
            return (int)(NormalizeLongitude(moon.Longitude - sun.Longitude) / 12);
        }

        private int NakshatraIndex(DateTime utc, GeoLocation location)
        {
            var moon = _ephemerisProvider.GetPlanet(Planet.Moon, utc, location);
            return (int)(NormalizeLongitude(moon.Longitude) / (360.0 / 27.0));
        }

        private int NakshatraPadaIndex(DateTime utc, GeoLocation location)
        {
            var moon = _ephemerisProvider.GetPlanet(Planet.Moon, utc, location);
            return Math.Clamp((int)(NormalizeLongitude(moon.Longitude) / (360.0 / 108.0)), 0, 107);
        }

        private int YogaIndex(DateTime utc, GeoLocation location)
        {
            var sun = _ephemerisProvider.GetPlanet(Planet.Sun, utc, location);
            var moon = _ephemerisProvider.GetPlanet(Planet.Moon, utc, location);
            return (int)(NormalizeLongitude(sun.Longitude + moon.Longitude) / (360.0 / 27.0));
        }

        private int KaranaIndex(DateTime utc, GeoLocation location)
        {
            var sun = _ephemerisProvider.GetPlanet(Planet.Sun, utc, location);
            var moon = _ephemerisProvider.GetPlanet(Planet.Moon, utc, location);
            return Math.Clamp((int)(NormalizeLongitude(moon.Longitude - sun.Longitude) / 6), 0, 10);
        }

        private static double NormalizeLongitude(double angle)
        {
            return (angle % 360 + 360) % 360;
        }

        private static double NormalizeSignedAngle(double angle)
        {
            var normalized = NormalizeLongitude(angle);
            return normalized > 180 ? normalized - 360 : normalized;
        }

        private static double DegreesToRadians(double degrees)
        {
            return degrees * Math.PI / 180.0;
        }

        private static double RadiansToDegrees(double radians)
        {
            return radians * 180.0 / Math.PI;
        }

        private static DateTime ConvertLocalToUtc(DateTime localDateTime, string timezone)
        {
            try
            {
                var timezoneInfo = TimeZoneInfo.FindSystemTimeZoneById(timezone);
                return TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(localDateTime, DateTimeKind.Unspecified), timezoneInfo);
            }
            catch (TimeZoneNotFoundException)
            {
                return localDateTime.AddHours(-5.5);
            }
            catch (InvalidTimeZoneException)
            {
                return localDateTime.AddHours(-5.5);
            }
        }

        private static TimeSpan GetTimezoneOffset(DateTime localDateTime, string timezone)
        {
            try
            {
                var timezoneInfo = TimeZoneInfo.FindSystemTimeZoneById(timezone);
                return timezoneInfo.GetUtcOffset(DateTime.SpecifyKind(localDateTime, DateTimeKind.Unspecified));
            }
            catch (TimeZoneNotFoundException)
            {
                return TimeSpan.FromHours(5.5);
            }
            catch (InvalidTimeZoneException)
            {
                return TimeSpan.FromHours(5.5);
            }
        }

        private static string FormatTime(DateTimeOffset dateTime)
        {
            return dateTime.ToString("HH:mm");
        }

        private static string FormatOptionalTime(DateTimeOffset? dateTime)
        {
            return dateTime?.ToString("HH:mm") ?? string.Empty;
        }

        private static bool TryNormalizeMode(string mode, out string normalizedMode)
        {
            normalizedMode = (mode ?? string.Empty).Trim().ToLowerInvariant();
            return normalizedMode is "daily" or "weekly" or "monthly";
        }

        private static (DateOnly StartDate, DateOnly EndDate) GetDateRange(string mode, DateOnly date)
        {
            return mode switch
            {
                "daily" => (date, date),
                "weekly" => GetWeekRange(date),
                "monthly" => GetMonthRange(date),
                _ => (date, date)
            };
        }

        private static (DateOnly StartDate, DateOnly EndDate) GetWeekRange(DateOnly date)
        {
            var daysSinceSunday = (int)date.DayOfWeek;
            var start = date.AddDays(-daysSinceSunday);
            return (start, start.AddDays(6));
        }

        private static (DateOnly StartDate, DateOnly EndDate) GetMonthRange(DateOnly date)
        {
            var start = new DateOnly(date.Year, date.Month, 1);
            var end = start.AddMonths(1).AddDays(-1);
            return (start, end);
        }

        private static bool IsValidLocation(double latitude, double longitude)
        {
            return latitude is >= -90 and <= 90 && longitude is >= -180 and <= 180;
        }

        private static string BuildSummary(PanchangDetails details)
        {
            return $"{details.Vaar}: {details.Paksha} {details.Tithi}, {details.Nakshatra} Nakshatra, {details.Yoga} Yoga, {details.Karana} Karana.";
        }
    }
}


