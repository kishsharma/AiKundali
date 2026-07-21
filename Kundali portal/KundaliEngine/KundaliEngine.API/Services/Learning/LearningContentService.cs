using System.Text.Json;
using KundaliEngine.API.DTOs.Learning;

namespace KundaliEngine.API.Services.Learning;

public sealed class LearningContentService : ILearningContentService
{
    private static readonly string[] SupportedLanguages = ["en", "hi", "kn"];
    private readonly string _contentRoot;
    private readonly ILogger<LearningContentService> _logger;
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly SemaphoreSlim _manifestLock = new(1, 1);
    private IReadOnlyList<LearningLevelManifest>? _cachedManifests;

    public LearningContentService(ILogger<LearningContentService> logger)
        : this(Path.Combine(AppContext.BaseDirectory, "LearningContent"), logger)
    {
    }

    public LearningContentService(string contentRoot, ILogger<LearningContentService> logger)
    {
        _contentRoot = contentRoot;
        _logger = logger;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };
    }

    public async Task<IReadOnlyList<LearningLevelSummary>> GetLevelsAsync(CancellationToken cancellationToken)
    {
        var manifests = await LoadManifestsAsync(cancellationToken);
        return manifests.Select(ToSummary).ToList();
    }

    public async Task<LearningLevelManifest?> GetLevelAsync(string levelCode, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(levelCode))
        {
            return null;
        }

        var manifests = await LoadManifestsAsync(cancellationToken);
        return manifests.FirstOrDefault(level => string.Equals(level.Code, levelCode.Trim(), StringComparison.OrdinalIgnoreCase));
    }

    public async Task<LearningSessionResponse?> GetSessionAsync(Guid sessionGuid, string? language, CancellationToken cancellationToken)
    {
        var manifests = await LoadManifestsAsync(cancellationToken);
        var match = manifests
            .SelectMany(level => level.Sessions.Select(session => new { Level = level, Session = session }))
            .FirstOrDefault(item => item.Session.Id == sessionGuid);

        if (match is null)
        {
            _logger.LogInformation("Learning session {SessionGuid} was not found in any manifest.", sessionGuid);
            return null;
        }

        var requestedLanguage = ResolveRequestedLanguage(language, match.Level.Languages.Default);
        var sessionPath = BuildSessionPath(match.Level.Code, match.Session.SessionNumber, requestedLanguage);
        var resolvedLanguage = requestedLanguage;
        var usedFallback = false;

        if (!File.Exists(sessionPath) && !string.Equals(requestedLanguage, "en", StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogWarning(
                "Learning session {SessionGuid} language {Language} was missing. Falling back to English.",
                sessionGuid,
                requestedLanguage);
            sessionPath = BuildSessionPath(match.Level.Code, match.Session.SessionNumber, "en");
            resolvedLanguage = "en";
            usedFallback = true;
        }

        if (!File.Exists(sessionPath))
        {
            _logger.LogWarning("Learning session file was missing for {SessionGuid} at {SessionPath}.", sessionGuid, sessionPath);
            return null;
        }

        try
        {
            await using var stream = File.OpenRead(sessionPath);
            var session = await JsonSerializer.DeserializeAsync<LearningSession>(stream, _jsonOptions, cancellationToken);
            if (session is null)
            {
                throw new InvalidOperationException("Learning session content is empty or invalid.");
            }

            if (session.Id != sessionGuid || session.Id != match.Session.Id)
            {
                throw new InvalidOperationException($"Learning session GUID mismatch for manifest session {match.Session.Id}.");
            }

            if (!string.Equals(session.LevelCode, match.Level.Code, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException($"Learning session level mismatch for session {sessionGuid}.");
            }

            var glossary = await LoadGlossaryAsync(sessionPath, cancellationToken);

            return new LearningSessionResponse
            {
                ResolvedLanguage = resolvedLanguage,
                UsedFallback = usedFallback,
                ManifestSession = match.Session,
                Session = session,
                Glossary = glossary
            };
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Learning session {SessionGuid} failed validation/deserialization.", sessionGuid);
            throw new InvalidOperationException($"Learning session content is invalid: {ex.Message}", ex);
        }
    }

    private async Task<IReadOnlyList<LearningLevelManifest>> LoadManifestsAsync(CancellationToken cancellationToken)
    {
        if (_cachedManifests is not null)
        {
            return _cachedManifests;
        }

        await _manifestLock.WaitAsync(cancellationToken);
        try
        {
            if (_cachedManifests is not null)
            {
                return _cachedManifests;
            }

            var levelsRoot = Path.Combine(_contentRoot, "Levels");
            if (!Directory.Exists(levelsRoot))
            {
                _logger.LogWarning("Learning levels directory does not exist at {LevelsRoot}.", levelsRoot);
                _cachedManifests = Array.Empty<LearningLevelManifest>();
                return _cachedManifests;
            }

            var manifests = new List<LearningLevelManifest>();
            foreach (var filePath in Directory.EnumerateFiles(levelsRoot, "*.json", SearchOption.TopDirectoryOnly))
            {
                cancellationToken.ThrowIfCancellationRequested();
                try
                {
                    await using var stream = File.OpenRead(filePath);
                    var manifest = await JsonSerializer.DeserializeAsync<LearningLevelManifest>(stream, _jsonOptions, cancellationToken);
                    if (manifest is not null)
                    {
                        manifests.Add(manifest);
                    }
                }
                catch (JsonException ex)
                {
                    _logger.LogWarning(ex, "Learning level manifest {ManifestPath} could not be loaded.", filePath);
                    throw new InvalidOperationException($"Learning level manifest is invalid: {Path.GetFileName(filePath)}", ex);
                }
            }

            _cachedManifests = manifests
                .OrderBy(level => level.Code, StringComparer.OrdinalIgnoreCase)
                .ToList();
            return _cachedManifests;
        }
        finally
        {
            _manifestLock.Release();
        }
    }

    private async Task<IReadOnlyList<LearningGlossaryEntry>> LoadGlossaryAsync(string sessionPath, CancellationToken cancellationToken)
    {
        var glossaryPath = Path.Combine(Path.GetDirectoryName(sessionPath) ?? _contentRoot, "glossary.json");
        if (!File.Exists(glossaryPath))
        {
            return Array.Empty<LearningGlossaryEntry>();
        }

        try
        {
            await using var stream = File.OpenRead(glossaryPath);
            var glossary = await JsonSerializer.DeserializeAsync<IReadOnlyList<LearningGlossaryEntry>>(stream, _jsonOptions, cancellationToken);
            return glossary ?? Array.Empty<LearningGlossaryEntry>();
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Learning glossary {GlossaryPath} failed validation/deserialization.", glossaryPath);
            throw new InvalidOperationException($"Learning glossary content is invalid: {Path.GetFileName(glossaryPath)}", ex);
        }
    }

    private string BuildSessionPath(string levelCode, int sessionNumber, string language)
    {
        var levelFolder = ToPascalFolder(levelCode);
        var sessionFolder = $"Session{sessionNumber:00}";
        var fileName = $"{levelCode}-session-{sessionNumber:00}.{language}.json";
        return Path.Combine(_contentRoot, "Sessions", levelFolder, sessionFolder, fileName);
    }

    private static string ResolveRequestedLanguage(string? language, string defaultLanguage)
    {
        var normalized = string.IsNullOrWhiteSpace(language) ? defaultLanguage : language.Trim().ToLowerInvariant();
        return SupportedLanguages.Contains(normalized, StringComparer.OrdinalIgnoreCase) ? normalized : "en";
    }

    private static string ToPascalFolder(string levelCode)
    {
        var parts = levelCode.Split('-', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return string.Concat(parts.Select(part => char.ToUpperInvariant(part[0]) + part[1..]));
    }

    private static LearningLevelSummary ToSummary(LearningLevelManifest manifest)
    {
        return new LearningLevelSummary
        {
            Id = manifest.Id,
            Code = manifest.Code,
            Slug = manifest.Slug,
            Title = manifest.Title,
            Subtitle = manifest.Subtitle,
            Description = manifest.Description,
            SessionCount = manifest.SessionCount,
            EstimatedMinutes = manifest.EstimatedMinutes,
            EstimatedHours = manifest.EstimatedHours,
            Languages = manifest.Languages,
            Status = manifest.Status
        };
    }
}