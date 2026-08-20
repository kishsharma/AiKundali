namespace KundaliEngine.API.DTOs.Responses
{
    public class PanchangApiResponse<T>
    {
        public bool Success { get; set; }
        public T? Data { get; set; }
        public string? Message { get; set; }
    }

    public class PanchangCalendarResponseDto
    {
        public string Mode { get; set; } = string.Empty;
        public DateOnly StartDate { get; set; }
        public DateOnly EndDate { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public string Timezone { get; set; } = string.Empty;
        public List<PanchangCalendarDayDto> Days { get; set; } = new();
    }

    public class PanchangEclipseYearResponseDto
    {
        public int Year { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public string Timezone { get; set; } = string.Empty;
        public List<PanchangEclipseDto> Eclipses { get; set; } = new();
    }

    public class PanchangEclipseDto
    {
        public string Id { get; set; } = string.Empty;
        public string Kind { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public DateOnly Date { get; set; }
        public DateTimeOffset StartTime { get; set; }
        public DateTimeOffset MaximumTime { get; set; }
        public DateTimeOffset EndTime { get; set; }
        public DateTimeOffset? SutakStartTime { get; set; }
        public DateTimeOffset? SutakEndTime { get; set; }
        public bool IsVisibleFromLocation { get; set; }
    }

    public class PanchangCalendarDayDto
    {
        public string Id { get; set; } = string.Empty;
        public DateOnly Date { get; set; }
        public string Tithi { get; set; } = string.Empty;
        public string TithiEndTime { get; set; } = string.Empty;
        public string Paksha { get; set; } = string.Empty;
        public string Nakshatra { get; set; } = string.Empty;
        public string NakshatraEndTime { get; set; } = string.Empty;
        public int Pada { get; set; }
        public string Akshara { get; set; } = string.Empty;
        public List<NakshatraPadaTimingDto> NakshatraPadas { get; set; } = new();
        public string Yoga { get; set; } = string.Empty;
        public string YogaEndTime { get; set; } = string.Empty;
        public string Karana { get; set; } = string.Empty;
        public string KaranaEndTime { get; set; } = string.Empty;
        public string Vaar { get; set; } = string.Empty;
        public string DayOfWeek { get; set; } = string.Empty;
        public string Maas { get; set; } = string.Empty;
        public string Shake { get; set; } = string.Empty;
        public string Samvat { get; set; } = string.Empty;
        public string SunRiseTime { get; set; } = string.Empty;
        public string SunSetTime { get; set; } = string.Empty;
        public string MoonRiseTime { get; set; } = string.Empty;
        public string MoonSetTime { get; set; } = string.Empty;
        public bool IsAuspicious { get; set; }
        public string Summary { get; set; } = string.Empty;
        public List<AuspiciousTimingDto> AuspiciousTimings { get; set; } = new();
    }

    public class PanchangLagnaTimingsResponseDto
    {
        public DateOnly Date { get; set; }
        public DateTimeOffset Sunrise { get; set; }
        public DateTimeOffset NextSunrise { get; set; }
        public List<LagnaTimingDto> Timings { get; set; } = new();
    }

    public class LagnaTimingDto
    {
        public string Sign { get; set; } = string.Empty;
        public string Lord { get; set; } = string.Empty;
        public DateTimeOffset EndTime { get; set; }
    }

    public class NakshatraPadaTimingDto
    {
        public int Pada { get; set; }
        public string Syllable { get; set; } = string.Empty;
        public string StartTime { get; set; } = string.Empty;
        public string EndTime { get; set; } = string.Empty;
        public bool IsCurrent { get; set; }
    }

    public class AuspiciousTimingDto
    {
        public string Type { get; set; } = string.Empty;
        public string StartTime { get; set; } = string.Empty;
        public string EndTime { get; set; } = string.Empty;
    }
}
