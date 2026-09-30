using KundaliEngine.API.DTOs.Muhurtha.GrahaPravesha;
using KundaliEngine.API.Services.Muhurtha;
using KundaliEngine.API.Services.Muhurtha.GrahaPravesha;
using KundaliEngine.API.Services.Muhurtha.Models;
using KundaliEngine.Astrology.Astronomy;
using KundaliEngine.Astrology.Services.Panchang;
using KundaliEngine.Domain.Enums;
using KundaliEngine.Domain.Models.Muhurtha;

namespace KundaliEngine.Muhurtha.Tests;

public class IntervalMathTests
{
    [Fact]
    public void Split_puts_tithi_change_on_the_boundary_without_gap_or_overlap()
    {
        var start = At(9, 0);
        var change = At(10, 32);
        var end = At(12, 0);
        var slices = PanchangBoundaryWalker.Split(
            start,
            end,
            instant => instant < change ? "A" : "B",
            TimeSpan.FromSeconds(1));

        Assert.Equal(2, slices.Count);
        Assert.Equal(start, slices[0].Start);
        Assert.Equal(change, slices[0].End);
        Assert.Equal("A", slices[0].Key);
        Assert.Equal(change, slices[1].Start);
        Assert.Equal(end, slices[1].End);
        Assert.Equal("B", slices[1].Key);
        Assert.Equal(slices[0].End, slices[1].Start);
    }

    [Fact]
    public void Subtract_removes_rahu_and_gulika_from_a_free_window()
    {
        var source = new TimeRange(At(9, 0), At(14, 0));
        var free = MuhurthaIntervalMath.Subtract(source,
        [
            new TimeRange(At(10, 30), At(12, 0)),
            new TimeRange(At(13, 0), At(14, 30))
        ]);

        Assert.Equal(2, free.Count);
        Assert.Equal(At(9, 0), free[0].Start);
        Assert.Equal(At(10, 30), free[0].End);
        Assert.Equal(At(12, 0), free[1].Start);
        Assert.Equal(At(13, 0), free[1].End);
    }

    [Fact]
    public void Leap_year_range_includes_29_february()
    {
        var dates = MuhurthaIntervalMath.EnumerateDates(new DateOnly(2024, 2, 28), new DateOnly(2024, 3, 1)).ToList();

        Assert.Contains(new DateOnly(2024, 2, 29), dates);
        Assert.Equal(3, dates.Count);
    }

    private static DateTimeOffset At(int hour, int minute)
    {
        return new DateTimeOffset(2027, 4, 12, hour, minute, 0, TimeSpan.FromHours(5.5));
    }
}

public class KalashaAndRuleTests
{
    private readonly GrahaPraveshaProfile _profile = LoadProfile();
    private readonly GrahaPraveshaRuleEngine _engine = new();

    [Theory]
    [InlineData(0, 4, 5, false)]
    [InlineData(0, 5, 6, true)]
    [InlineData(0, 12, 13, true)]
    [InlineData(0, 13, 14, false)]
    [InlineData(0, 20, 21, false)]
    [InlineData(0, 21, 22, true)]
    [InlineData(1, 0, 27, true)]
    public void Kalasha_ranges_include_the_configured_edges(int sun, int moon, int expectedCount, bool eligible)
    {
        var count = KalashaChakraEvaluator.CountInclusive(sun, moon);
        Assert.Equal(expectedCount, count);
        Assert.Equal(eligible, KalashaChakraEvaluator.IsEligible(count, _profile.Kalasha.FavorableRanges));
    }

    [Fact]
    public void Hard_exclusion_rejects_even_when_soft_score_is_high()
    {
        var outcome = _engine.Evaluate(GoodFacts() with { Tithi = "Chaturthi", VastuTableMatched = true }, _profile);

        Assert.Equal("REJECTED", outcome.Status);
        Assert.True(outcome.HardRejected);
        Assert.True(outcome.Score >= _profile.Scoring.RecommendedMinimum);
        Assert.Contains(outcome.Evidence, item => item.RuleId == "GP_TITHI_RIKTA" && item.Result == "FAIL");
    }

    [Fact]
    public void Amavasya_vyatipata_vishti_and_prohibited_lagna_reject()
    {
        Assert.Equal("REJECTED", _engine.Evaluate(GoodFacts() with { Tithi = "Amavasya" }, _profile).Status);
        Assert.Equal("REJECTED", _engine.Evaluate(GoodFacts() with { Yoga = "Vyatipata" }, _profile).Status);
        Assert.Equal("REJECTED", _engine.Evaluate(GoodFacts() with { Karana = "Visti" }, _profile).Status);
        Assert.Equal("REJECTED", _engine.Evaluate(GoodFacts() with { Lagna = "Aries" }, _profile).Status);
        Assert.All(new[] { "Cancer", "Libra", "Capricorn" }, sign =>
            Assert.Equal("REJECTED", _engine.Evaluate(GoodFacts() with { Lagna = sign }, _profile).Status));
    }

    [Fact]
    public void Vastu_miss_stays_eligible_and_a_match_scores_higher()
    {
        var missed = _engine.Evaluate(GoodFacts() with { VastuTableMatched = false }, _profile);
        var matched = _engine.Evaluate(GoodFacts() with { VastuTableMatched = true }, _profile);

        Assert.NotEqual("REJECTED", missed.Status);
        Assert.True(matched.Score > missed.Score);
        Assert.Contains(matched.Evidence, item => item.RuleId == "GP_VASTU_TABLE" && item.ScoreImpact == 18);
    }

    [Fact]
    public void Disabled_unverified_rules_do_not_reject()
    {
        var outcome = _engine.Evaluate(GoodFacts(), _profile);

        Assert.NotEqual("REJECTED", outcome.Status);
        foreach (var ruleId in new[] { "GP_PANCHAKA", "GP_VRISHA", "GP_DUR_MUHURTA", "GP_VARJYAM", "GP_GANDANTA" })
        {
            Assert.Contains(outcome.Evidence, item => item.RuleId == ruleId && item.Result == "SKIPPED");
        }
    }

    [Fact]
    public void Ineligible_kalasha_blocks_recommended_without_rejecting()
    {
        var outcome = _engine.Evaluate(GoodFacts() with { KalashaCount = 5, VastuTableMatched = true }, _profile);

        Assert.NotEqual("REJECTED", outcome.Status);
        Assert.NotEqual("RECOMMENDED", outcome.Status);
        Assert.Contains(outcome.Evidence, item => item.RuleId == "GP_CHK_001" && item.Result == "WARNING");
    }

    private static GrahaPraveshaFacts GoodFacts()
    {
        return new GrahaPraveshaFacts
        {
            Tithi = "Panchami",
            Paksha = "Shukla",
            Nakshatra = "Rohini",
            Yoga = "Shobhana",
            Karana = "Bava",
            Vara = "Monday",
            MoonSign = "Taurus",
            Lagna = "Taurus",
            KalashaCount = 8,
            VastuTableMatched = false
        };
    }

    private static GrahaPraveshaProfile LoadProfile()
    {
        var directory = Path.Combine(AppContext.BaseDirectory, "Data", "Muhurtha");
        return new GrahaPraveshaProfileProvider(directory).GetProfile();
    }
}

public class TimezoneAndPanchangFactTests
{
    [Fact]
    public void London_dst_changes_the_offset_around_the_spring_transition()
    {
        var before = GrahaPraveshaTime.GetOffset("Europe/London", new DateOnly(2026, 3, 28));
        var after = GrahaPraveshaTime.GetOffset("Europe/London", new DateOnly(2026, 3, 30));

        Assert.NotEqual(before, after);
        Assert.Equal(TimeSpan.FromHours(1), after - before);
    }

    [Fact]
    public void India_offset_stays_five_and_a_half_hours_on_a_leap_day()
    {
        var offset = GrahaPraveshaTime.GetOffset("Asia/Kolkata", new DateOnly(2024, 2, 29));

        Assert.Equal(TimeSpan.FromHours(5.5), offset);
    }

    [Theory]
    [InlineData("Bengaluru", 12.9716, 77.5946, "Asia/Kolkata")]
    [InlineData("Mumbai", 19.0760, 72.8777, "Asia/Kolkata")]
    [InlineData("Delhi", 28.6139, 77.2090, "Asia/Kolkata")]
    [InlineData("London", 51.5074, -0.1278, "Europe/London")]
    public void Fixed_longitudes_and_sunrise_are_stable_for_each_city(string city, double latitude, double longitude, string timezone)
    {
        var panchang = new PanchangService().Calculate(new DateTime(2026, 4, 12, 9, 0, 0), 15, 45);
        Assert.Equal("Tritiya", panchang.Tithi);
        Assert.Equal("Shukla", panchang.Paksha);
        Assert.Equal("Rohini", panchang.Nakshatra);
        Assert.Equal("Shobhana", panchang.Yoga);
        Assert.Equal("Vanija", panchang.Karana);

        var offset = GrahaPraveshaTime.GetOffset(timezone, new DateOnly(2026, 4, 12));
        var (sunrise, sunset) = new MuhurthaSunriseSunsetService().Calculate(
            new DateOnly(2026, 4, 12),
            new MuhurthaLocation { Name = city, Latitude = latitude, Longitude = longitude, Timezone = timezone },
            offset);

        Assert.True(sunset > sunrise);
        Assert.Equal(offset, sunrise.Offset);
        Assert.InRange(sunrise.Hour, 4, 8);
        Assert.InRange(sunset.Hour, 17, 21);

        try
        {
            var provider = new SwissEphemerisProvider();
            var utc = new DateTime(2026, 4, 12, 3, 30, 0, DateTimeKind.Utc);
            var location = new GeoLocation(latitude, longitude);
            var first = provider.GetPlanet(Planet.Sun, utc, location);
            var second = provider.GetPlanet(Planet.Sun, utc, location);
            Assert.Equal(first.Longitude, second.Longitude);
            Assert.InRange(first.Longitude, 0, 360);
        }
        catch (Exception ex) when (ex is DllNotFoundException or FileNotFoundException or TypeInitializationException)
        {
            Assert.True(true, $"Ephemeris native library was not available for {city}: {ex.Message}");
        }
    }
}

public class GrahaPraveshaSearchPipelineTests
{
    [Fact]
    public void Search_splits_on_tithi_and_drops_rahu_and_rikta()
    {
        var dataDirectory = Path.Combine(AppContext.BaseDirectory, "Data", "Muhurtha");
        var change = new DateTimeOffset(2027, 4, 12, 10, 32, 0, TimeSpan.FromHours(5.5));
        var sunrise = new DateTimeOffset(2027, 4, 12, 6, 0, 0, TimeSpan.FromHours(5.5));
        var sunset = new DateTimeOffset(2027, 4, 12, 18, 0, 0, TimeSpan.FromHours(5.5));
        var rahuStart = new DateTimeOffset(2027, 4, 12, 10, 30, 0, TimeSpan.FromHours(5.5));
        var rahuEnd = new DateTimeOffset(2027, 4, 12, 12, 0, 0, TimeSpan.FromHours(5.5));

        var service = new GrahaPraveshaMuhurthaService(
            new GrahaPraveshaProfileProvider(dataDirectory),
            new MuhurthaMetadataService(dataDirectory),
            new FixedSunriseService(sunrise, sunset),
            new FixedWindowService(rahuStart, rahuEnd),
            new ScriptedSnapshotReader(change),
            new GrahaPraveshaRuleEngine(),
            new UnusedBirthEvaluator());

        var response = service.Search(new GrahaPraveshaSearchRequestDto
        {
            Location = new GrahaPraveshaLocationDto
            {
                City = "Bengaluru",
                Latitude = 12.9716,
                Longitude = 77.5946,
                Timezone = "Asia/Kolkata"
            },
            FromDate = new DateOnly(2027, 4, 12),
            ToDate = new DateOnly(2027, 4, 12),
            CalendarSystem = "AMANTA"
        });

        Assert.DoesNotContain(response.Results, item => item.Tithi == "Chaturthi");
        Assert.Contains(response.Results, item => item.Tithi == "Panchami" && item.Status != "REJECTED");
        Assert.Contains(response.RejectionCounts.Keys, key => key is "GP_TITHI_RIKTA" or "GP_RAHU_KALAM");
        Assert.All(response.Results, item => Assert.True(item.StartTime < item.EndTime));

        var day = service.AnalyzeDate(
            new DateOnly(2027, 4, 12),
            response.Location,
            "AMANTA",
            null);
        var rahuPieces = day.Intervals.Where(item =>
            item.Evidence.Any(evidence => evidence.RuleId == "GP_RAHU_KALAM" && evidence.Result == "FAIL")).ToList();
        Assert.NotEmpty(rahuPieces);
        Assert.All(rahuPieces, item => Assert.Equal("REJECTED", item.Status));
        Assert.Contains(day.Intervals, item => item.Evidence.Any(evidence => evidence.RuleId == "GP_PANCHAKA" && evidence.Result == "SKIPPED"));
    }

    private sealed class FixedSunriseService : IMuhurthaSunriseSunsetService
    {
        private readonly DateTimeOffset _sunrise;
        private readonly DateTimeOffset _sunset;

        public FixedSunriseService(DateTimeOffset sunrise, DateTimeOffset sunset)
        {
            _sunrise = sunrise;
            _sunset = sunset;
        }

        public (DateTimeOffset Sunrise, DateTimeOffset Sunset) Calculate(DateOnly date, MuhurthaLocation location, TimeSpan offset)
        {
            return (_sunrise, _sunset);
        }
    }

    private sealed class FixedWindowService : IMuhurthaGenericTimeWindowService
    {
        private readonly DateTimeOffset _start;
        private readonly DateTimeOffset _end;

        public FixedWindowService(DateTimeOffset start, DateTimeOffset end)
        {
            _start = start;
            _end = end;
        }

        public IReadOnlyList<MuhurthaTimeWindow> BuildWindows(MuhurthaDayContext dayContext)
        {
            return
            [
                new MuhurthaTimeWindow
                {
                    RuleKey = "rahu_kalam",
                    DisplayName = "Rahu Kalam",
                    StartTime = _start,
                    EndTime = _end
                }
            ];
        }
    }

    private sealed class ScriptedSnapshotReader : IPanchangSnapshotReader
    {
        private readonly DateTimeOffset _change;

        public ScriptedSnapshotReader(DateTimeOffset change)
        {
            _change = change;
        }

        public PanchangSnapshot Read(DateTimeOffset instant, double latitude, double longitude)
        {
            var before = instant < _change;
            return new PanchangSnapshot
            {
                Tithi = before ? "Chaturthi" : "Panchami",
                Paksha = "Shukla",
                Nakshatra = "Rohini",
                Yoga = "Shobhana",
                Karana = "Bava",
                MoonSign = "Taurus",
                Lagna = "Taurus",
                SunNakshatraIndex = 0,
                MoonNakshatraIndex = 7
            };
        }
    }

    private sealed class UnusedBirthEvaluator : IGrahaPraveshaBirthChartEvaluator
    {
        public IReadOnlyList<GrahaPraveshaEvidence> Evaluate(
            DateTime birthLocal,
            string birthTimezone,
            double birthLatitude,
            double birthLongitude,
            DateTimeOffset eventInstant,
            double eventLatitude,
            double eventLongitude,
            out int scoreDelta)
        {
            scoreDelta = 0;
            return Array.Empty<GrahaPraveshaEvidence>();
        }
    }
}
