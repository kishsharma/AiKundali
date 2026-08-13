using System.Text.Json.Nodes;
using KundaliEngine.API.Services.Auth;
using KundaliEngine.API.Services.Learning;
using Microsoft.Extensions.Logging;

var Session01Id = Guid.Parse("a3c9eff3-380b-5c4b-9135-6ea4e543d528");

var sourceRoot = Path.GetFullPath(Path.Combine(
    AppContext.BaseDirectory,
    "..",
    "..",
    "..",
    "..",
    "KundaliEngine.KnowledgeBase",
    "LearningContent"));

var tests = new List<(string Name, Func<Task> Test)>
{
    ("English session load", TestEnglishSessionLoad),
    ("Session glossary load", TestSessionGlossaryLoad),
    ("Hindi session load", TestHindiSessionLoad),
    ("Kannada session load", TestKannadaSessionLoad),
    ("Missing language file falls back to English", TestMissingLanguageFallback),
    ("Invalid GUID returns not found", TestInvalidGuid),
    ("Unsupported language falls back to English", TestUnsupportedLanguageFallback),
    ("Manifest/session GUID mismatch", TestManifestSessionGuidMismatch),
    ("Unknown block validation", TestUnknownBlockValidation),
    ("Preferred language normalization", TestPreferredLanguageNormalization)
};

foreach (var test in tests)
{
    await test.Test();
    Console.WriteLine($"[PASS] {test.Name}");
}

async Task TestEnglishSessionLoad()
{
    var service = BuildService(sourceRoot);
    var response = await service.GetSessionAsync(Session01Id, "en", CancellationToken.None);
    Assert(response is not null, "English session should load.");
    AssertEqual("en", response!.ResolvedLanguage, "Resolved language should be English.");
    AssertEqual(Session01Id, response.Session.Id, "Session id should match manifest.");
}

async Task TestSessionGlossaryLoad()
{
    var service = BuildService(sourceRoot);
    var response = await service.GetSessionAsync(Session01Id, "en", CancellationToken.None);
    Assert(response is not null, "Session should load.");
    Assert(response!.Glossary.Count > 0, "Session glossary should load.");
    var jyotisha = response.Glossary.FirstOrDefault(entry => entry.Key == "jyotisha");
    Assert(jyotisha is not null, "Jyotisha glossary entry should exist.");
    AssertEqual("Jyotisha", jyotisha!.Term["en"], "English glossary term should load.");
    Assert(jyotisha.Term.ContainsKey("hi"), "Hindi glossary term should load.");
    Assert(jyotisha.Term.ContainsKey("kn"), "Kannada glossary term should load.");
}
async Task TestHindiSessionLoad()
{
    var service = BuildService(sourceRoot);
    var response = await service.GetSessionAsync(Session01Id, "hi", CancellationToken.None);
    Assert(response is not null, "Hindi session should load.");
    AssertEqual("hi", response!.ResolvedLanguage, "Resolved language should be Hindi.");
}

async Task TestKannadaSessionLoad()
{
    var service = BuildService(sourceRoot);
    var response = await service.GetSessionAsync(Session01Id, "kn", CancellationToken.None);
    Assert(response is not null, "Kannada session should load.");
    AssertEqual("kn", response!.ResolvedLanguage, "Resolved language should be Kannada.");
}

async Task TestMissingLanguageFallback()
{
    var tempRoot = CopyContentToTemp();
    File.Delete(Path.Combine(tempRoot, "Sessions", "Level1", "Session01", "level-1-session-01.hi.json"));
    var service = BuildService(tempRoot);
    var response = await service.GetSessionAsync(Session01Id, "hi", CancellationToken.None);
    Assert(response is not null, "Fallback session should load.");
    AssertEqual("en", response!.ResolvedLanguage, "Missing Hindi file should resolve to English.");
    Assert(response.UsedFallback, "Missing requested language should mark fallback.");
}

async Task TestInvalidGuid()
{
    var service = BuildService(sourceRoot);
    var response = await service.GetSessionAsync(Guid.NewGuid(), "en", CancellationToken.None);
    Assert(response is null, "Invalid GUID should return null/not found result.");
}

async Task TestUnsupportedLanguageFallback()
{
    var service = BuildService(sourceRoot);
    var response = await service.GetSessionAsync(Session01Id, "fr", CancellationToken.None);
    Assert(response is not null, "Unsupported language should still load English.");
    AssertEqual("en", response!.ResolvedLanguage, "Unsupported language should resolve to English.");
}

async Task TestManifestSessionGuidMismatch()
{
    var tempRoot = CopyContentToTemp();
    var sessionPath = Path.Combine(tempRoot, "Sessions", "Level1", "Session01", "level-1-session-01.en.json");
    var json = JsonNode.Parse(await File.ReadAllTextAsync(sessionPath))!.AsObject();
    json["id"] = Guid.NewGuid().ToString();
    await File.WriteAllTextAsync(sessionPath, json.ToJsonString());

    var service = BuildService(tempRoot);
    await AssertThrowsAsync<InvalidOperationException>(() => service.GetSessionAsync(Session01Id, "en", CancellationToken.None));
}

async Task TestUnknownBlockValidation()
{
    var tempRoot = CopyContentToTemp();
    var sessionPath = Path.Combine(tempRoot, "Sessions", "Level1", "Session01", "level-1-session-01.en.json");
    var json = JsonNode.Parse(await File.ReadAllTextAsync(sessionPath))!.AsObject();
    json["sections"]![0]!["blocks"]![0]!["type"] = "unknown-block";
    await File.WriteAllTextAsync(sessionPath, json.ToJsonString());

    var service = BuildService(tempRoot);
    await AssertThrowsAsync<InvalidOperationException>(() => service.GetSessionAsync(Session01Id, "en", CancellationToken.None));
}


Task TestPreferredLanguageNormalization()
{
    AssertEqual("en", PreferredLanguageHelper.Normalize("en"), "English should normalize.");
    AssertEqual("hi", PreferredLanguageHelper.Normalize(" HI "), "Hindi should normalize casing and whitespace.");
    AssertEqual("kn", PreferredLanguageHelper.Normalize("kn"), "Kannada should normalize.");
    Assert(PreferredLanguageHelper.Normalize(null) is null, "Null preference should stay null.");
    Assert(PreferredLanguageHelper.Normalize("fr") is null, "Unsupported language should not normalize.");
    AssertEqual("hi", PreferredLanguageHelper.NormalizeRequired("hi"), "Required language should return normalized value.");
    AssertThrows<InvalidOperationException>(() => PreferredLanguageHelper.NormalizeRequired("fr"));
    return Task.CompletedTask;
}
LearningContentService BuildService(string root) => new(root, new NullLogger<LearningContentService>());

string CopyContentToTemp()
{
    var target = Path.Combine(Path.GetTempPath(), "learning-content-tests", Guid.NewGuid().ToString("N"));
    CopyDirectory(sourceRoot, target);
    return target;
}

void CopyDirectory(string source, string target)
{
    Directory.CreateDirectory(target);
    foreach (var file in Directory.GetFiles(source))
    {
        File.Copy(file, Path.Combine(target, Path.GetFileName(file)), overwrite: true);
    }

    foreach (var directory in Directory.GetDirectories(source))
    {
        CopyDirectory(directory, Path.Combine(target, Path.GetFileName(directory)));
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
static async Task AssertThrowsAsync<TException>(Func<Task> action)
    where TException : Exception
{
    try
    {
        await action();
    }
    catch (TException)
    {
        return;
    }

    throw new InvalidOperationException($"Expected exception {typeof(TException).Name}.");
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





