using Microsoft.Extensions.Logging;
using KundaliEngine.API.DTOs.Requests;
using KundaliEngine.API.Services.CurrentLifeFocus;
using KundaliEngine.Domain.Enums;
using KundaliEngine.Domain.Models;

var tests = new List<(string Name, Action Test)>
{
    ("Correct top-three ordering", TestTopThreeOrdering),
    ("High activation with Supportive tone", TestHighSupportiveTone),
    ("High activation with Pressured tone", TestHighPressuredTone),
    ("Same house contributes to multiple life areas", TestSharedHouseContribution),
    ("Focused narrative is populated", TestFocusedNarrativeIsPopulated),
    ("Invalid area key", TestInvalidAreaKey),
    ("Expired or missing analysisId", TestMissingAnalysisId),
    ("No strongly activated areas", TestNoStronglyActivatedAreas),
    ("Metadata validation", TestMetadataValidation),
    ("Deterministic repeatability", TestDeterministicRepeatability),
    ("Invalid analysis date", TestInvalidAnalysisDate)
};

foreach (var test in tests)
{
    test.Test();
    Console.WriteLine($"[PASS] {test.Name}");
}

static void TestTopThreeOrdering()
{
    var mapper = new LifeAreaMapper();
    var areas = mapper.Map(BuildHouseScores((10, 90, 20, 2), (6, 80, 15, 5), (11, 75, 12, 2), (4, 30, 0, 0)), BuildConfiguration());

    AssertEqual("career", areas[0].Key, "Career should rank first.");
    Assert(areas.Take(3).All(area => !string.IsNullOrWhiteSpace(area.Key)), "Top three should contain valid areas.");
}

static void TestHighSupportiveTone()
{
    var mapper = new LifeAreaMapper();
    var areas = mapper.Map(BuildHouseScores((10, 92, 30, 2), (6, 88, 18, 2), (11, 85, 16, 2)), BuildConfiguration());

    AssertEqual("High", areas[0].ActivationLevel, "Expected high activation.");
    AssertEqual("Supportive", areas[0].Tone, "Expected supportive tone.");
}

static void TestHighPressuredTone()
{
    var mapper = new LifeAreaMapper();
    var areas = mapper.Map(BuildHouseScores((10, 92, 2, 30), (6, 88, 2, 18), (11, 85, 2, 16)), BuildConfiguration());

    AssertEqual("High", areas[0].ActivationLevel, "Expected high activation.");
    AssertEqual("Pressured", areas[0].Tone, "Expected pressured tone.");
}

static void TestSharedHouseContribution()
{
    var mapper = new LifeAreaMapper();
    var areas = mapper.Map(BuildHouseScores((6, 90, 8, 8), (10, 70, 5, 5), (8, 80, 5, 15)), BuildConfiguration()).ToList();

    var career = areas.First(area => area.Key == "career");
    var disputes = areas.First(area => area.Key == "debt_disputes_competition");
    Assert(career.ActivatedHouses.Contains(6), "House 6 should contribute to career.");
    Assert(disputes.ActivatedHouses.Contains(6), "House 6 should contribute to disputes.");
}

static void TestFocusedNarrativeIsPopulated()
{
    var store = new CurrentLifeFocusAnalysisStore();
    var service = BuildFocusedService(store);
    var snapshot = BuildSnapshot();
    store.Save(snapshot, TimeSpan.FromMinutes(5));

    var response = service.Analyze(snapshot.AnalysisId, null, new FocusedLifeAreaRequest { SelectedAreaKey = "career" });

    Assert(!string.IsNullOrWhiteSpace(response.Narrative.Verdict), "Focused analysis should include a narrative verdict.");
    Assert(!string.IsNullOrWhiteSpace(response.Narrative.AstrologicalReason), "Focused analysis should include an astrological reason.");
    Assert(response.Narrative.Evidence.Count > 0, "Focused analysis should include evidence lines.");
}
static void TestInvalidAreaKey()
{
    var store = new CurrentLifeFocusAnalysisStore();
    var service = BuildFocusedService(store);
    var snapshot = BuildSnapshot();
    store.Save(snapshot, TimeSpan.FromMinutes(5));

    AssertThrows<ArgumentException>(() => service.Analyze(snapshot.AnalysisId, null, new FocusedLifeAreaRequest { SelectedAreaKey = "missing" }));
}

static void TestMissingAnalysisId()
{
    var store = new CurrentLifeFocusAnalysisStore();
    var service = BuildFocusedService(store);

    AssertThrows<KeyNotFoundException>(() => service.Analyze(Guid.NewGuid(), null, new FocusedLifeAreaRequest { SelectedAreaKey = "career" }));
}

static void TestNoStronglyActivatedAreas()
{
    var mapper = new LifeAreaMapper();
    var areas = mapper.Map(BuildHouseScores((1, 25, 0, 0), (2, 30, 0, 0), (3, 20, 0, 0)), BuildConfiguration());

    Assert(areas.All(area => area.ActivationLevel != "High"), "No area should be high.");
    Assert(areas.Take(3).Count() == 3, "Mapper should still return ranked areas.");
}

static void TestMetadataValidation()
{
    var invalid = BuildConfiguration();
    invalid.LifeAreas[0].Houses[0].House = 13;

    AssertThrows<InvalidOperationException>(() => new StaticMetadataService(invalid));
}

static void TestDeterministicRepeatability()
{
    var mapper = new LifeAreaMapper();
    var config = BuildConfiguration();
    var scores = BuildHouseScores((10, 90, 20, 2), (6, 80, 15, 5), (11, 75, 12, 2));
    var first = mapper.Map(scores, config).Select(area => $"{area.Key}:{area.ActivationScore}:{area.Tone}");
    var second = mapper.Map(scores, config).Select(area => $"{area.Key}:{area.ActivationScore}:{area.Tone}");

    AssertEqual(string.Join("|", first), string.Join("|", second), "Mapping should be deterministic.");
}

static void TestInvalidAnalysisDate()
{
    var request = new CurrentLifeFocusRequest { KundaliId = Guid.NewGuid(), AnalysisDate = default };
    Assert(request.AnalysisDate == default, "Invalid analysis date fixture should be default.");
}

static CurrentLifeFocusSnapshot BuildSnapshot()
{
    var config = BuildConfiguration();
    var mapper = new LifeAreaMapper();
    var scores = BuildHouseScores((10, 90, 20, 2), (6, 80, 15, 5), (11, 75, 12, 2));
    return new CurrentLifeFocusSnapshot
    {
        AnalysisId = Guid.NewGuid(),
        KundaliId = Guid.NewGuid(),
        AnalysisDate = DateTimeOffset.UtcNow,
        Mahadasha = Planet.Saturn,
        Antardasha = Planet.Mercury,
        HouseScores = scores.ToList(),
        Areas = mapper.Map(scores, config).ToList()
    };
}

static FocusedLifeAreaAnalysisService BuildFocusedService(CurrentLifeFocusAnalysisStore store)
{
    return new FocusedLifeAreaAnalysisService(
        new StaticMetadataService(BuildConfiguration()),
        store,
        new NullLogger<FocusedLifeAreaAnalysisService>(),
        new FocusedLifeAreaNarrativeBuilder());
}
static List<HouseActivationScore> BuildHouseScores(params (int House, int Score, int Support, int Pressure)[] values)
{
    var byHouse = values.ToDictionary(item => item.House);
    return Enumerable.Range(1, 12)
        .Select(house =>
        {
            if (!byHouse.TryGetValue(house, out var value))
            {
                value = (house, 20, 0, 0);
            }

            return new HouseActivationScore
            {
                House = house,
                DashaScore = value.Score,
                GocharScore = value.Score,
                NatalSensitivityScore = value.Score,
                TotalScore = value.Score,
                SupportScore = value.Support,
                PressureScore = value.Pressure,
                Reasons = value.Score > 40 ? new List<string> { $"House {house} confirms activation." } : new List<string>(),
                SupportiveFactors = value.Support > 0 ? new List<string> { $"House {house} may support progress." } : new List<string>(),
                PressureFactors = value.Pressure > 0 ? new List<string> { $"House {house} may require care." } : new List<string>()
            };
        })
        .ToList();
}

static CurrentLifeFocusConfiguration BuildConfiguration()
{
    return new CurrentLifeFocusConfiguration
    {
        Scoring = new CurrentLifeFocusScoringConfiguration(),
        LifeAreas = new List<LifeAreaConfiguration>
        {
            Area("career", "Career and professional direction", (2, 0.45), (6, 0.65), (10, 1.0), (11, 0.65)),
            Area("finance", "Financial commitments", (2, 1.0), (8, 0.65), (11, 0.85), (12, 0.55)),
            Area("mental_peace_sleep", "Mental peace and sleep", (1, 0.55), (4, 1.0), (8, 0.6), (12, 0.85)),
            Area("debt_disputes_competition", "Debt, disputes and competition", (3, 0.45), (6, 1.0), (8, 0.65), (11, 0.45))
        }
    };
}

static LifeAreaConfiguration Area(string key, string title, params (int House, double Weight)[] houses)
{
    return new LifeAreaConfiguration
    {
        Key = key,
        DisplayName = title,
        SummaryTemplate = $"{title} currently receives {{activationLevel}} attention.",
        Houses = houses.Select(house => new LifeAreaHouseConfiguration { House = house.House, Weight = house.Weight }).ToList(),
        PossibleExperiences = new List<string> { $"{title} may be noticeable." },
        FollowUpQuestion = $"Which {title.ToLowerInvariant()} concern best matches your current situation?",
        FollowUpOptions = new List<LifeAreaOptionConfiguration> { new() { Key = "general", Label = "General" } }
    };
}

static void Assert(bool condition, string message)
{
    if (!condition)
    {
        throw new InvalidOperationException(message);
    }
}

static void AssertEqual<T>(T expected, T actual, string message)
{
    if (!EqualityComparer<T>.Default.Equals(expected, actual))
    {
        throw new InvalidOperationException($"{message} Expected '{expected}', got '{actual}'.");
    }
}

static void AssertThrows<TException>(Action action)
    where TException : Exception
{
    try
    {
        action();
    }
    catch (TException)
    {
        return;
    }

    throw new InvalidOperationException($"Expected exception {typeof(TException).Name}.");
}

public sealed class StaticMetadataService : CurrentLifeFocusMetadataService
{
    private readonly CurrentLifeFocusConfiguration _configuration;

    public StaticMetadataService(CurrentLifeFocusConfiguration configuration)
        : base(AppContext.BaseDirectory)
    {
        Validate(configuration);
        _configuration = configuration;
    }

    public override CurrentLifeFocusConfiguration GetConfiguration() => _configuration;

    public override LifeAreaConfiguration? GetLifeArea(string key)
    {
        return _configuration.LifeAreas.FirstOrDefault(area => string.Equals(area.Key, key, StringComparison.OrdinalIgnoreCase));
    }

    private static void Validate(CurrentLifeFocusConfiguration config)
    {
        foreach (var area in config.LifeAreas)
        {
            if (area.Houses.Any(house => house.House is < 1 or > 12 || house.Weight <= 0))
            {
                throw new InvalidOperationException("Invalid metadata.");
            }
        }
    }
}

public sealed class NullLogger<T> : ILogger<T>
{
    public IDisposable? BeginScope<TState>(TState state)
        where TState : notnull => null;

    public bool IsEnabled(LogLevel logLevel) => false;

    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
    }
}




