using System.Text.Json;
using System.Text.Json.Serialization;

namespace KundaliEngine.API.DTOs.Learning;

public class LearningLevelSummary
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Subtitle { get; set; }
    public string Description { get; set; } = string.Empty;
    public int SessionCount { get; set; }
    public int EstimatedMinutes { get; set; }
    public double EstimatedHours { get; set; }
    public LearningLanguages Languages { get; set; } = new();
    public string Status { get; set; } = string.Empty;
}

public sealed class LearningLevelManifest : LearningLevelSummary
{
    public IReadOnlyList<string> Audience { get; set; } = Array.Empty<string>();
    public string LearningOutcome { get; set; } = string.Empty;
    public LearningRecommendedPace? RecommendedPace { get; set; }
    public LearningAccess? Access { get; set; }
    public LearningNavigation? Navigation { get; set; }
    public LearningContentRules? ContentRules { get; set; }
    public LearningCompletionRules? CompletionRules { get; set; }
    public IReadOnlyList<LearningSessionSummary> Sessions { get; set; } = Array.Empty<LearningSessionSummary>();
}

public sealed class LearningRecommendedPace
{
    public int SessionsPerWeek { get; set; }
    public int EstimatedWeeks { get; set; }
    public string? Note { get; set; }
}

public sealed class LearningAccess
{
    public IReadOnlyList<string> RequiredRoles { get; set; } = Array.Empty<string>();
    public bool AuthenticationRequired { get; set; }
    public bool AllowPublicPreview { get; set; }
    public IReadOnlyList<Guid> PublicPreviewSessionIds { get; set; } = Array.Empty<Guid>();
}

public sealed class LearningLanguages
{
    public string Default { get; set; } = "en";
    public IReadOnlyList<string> Supported { get; set; } = Array.Empty<string>();
}

public sealed class LearningNavigation
{
    public string? Route { get; set; }
    public string? SessionRoutePattern { get; set; }
    public bool ShowProgress { get; set; }
    public bool ShowLockedSessions { get; set; }
    public bool AllowSequentialNavigation { get; set; }
}

public sealed class LearningContentRules
{
    public string? SessionSchema { get; set; }
    public string? ReferenceLibrary { get; set; }
    public bool OneJsonPerSessionPerLanguage { get; set; }
    public int MaximumShlokasPerSession { get; set; }
    public string? ShlokaDisplayMode { get; set; }
    public bool ReferencesRequired { get; set; }
    public bool CommonMistakesRequired { get; set; }
    public bool SelfCheckRequired { get; set; }
}

public sealed class LearningCompletionRules
{
    public bool RequireAllSessions { get; set; }
    public int MinimumQuizScorePercent { get; set; }
    public bool CertificateEnabled { get; set; }
    public string? CompletionMessage { get; set; }
}

public sealed class LearningSessionSummary
{
    public Guid Id { get; set; }
    public int SessionNumber { get; set; }
    public string Slug { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public int EstimatedMinutes { get; set; }
    public string Difficulty { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public Dictionary<string, string> Languages { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    public string? ContentPathPattern { get; set; }
    public IReadOnlyList<Guid> PrerequisiteSessionIds { get; set; } = Array.Empty<Guid>();
    public bool IsPreviewAvailable { get; set; }
}

public sealed class LearningSessionResponse
{
    public string ResolvedLanguage { get; set; } = "en";
    public bool UsedFallback { get; set; }
    public LearningSessionSummary? ManifestSession { get; set; }
    public LearningSession Session { get; set; } = new();
    public IReadOnlyList<LearningGlossaryEntry> Glossary { get; set; } = Array.Empty<LearningGlossaryEntry>();
}

public sealed class LearningKnowledgeMapCatalog
{
    public IReadOnlyList<LearningKnowledgeMapNodeCatalog> Nodes { get; set; } = Array.Empty<LearningKnowledgeMapNodeCatalog>();
    public IReadOnlyList<LearningKnowledgeMapConnectionCatalog> Connections { get; set; } = Array.Empty<LearningKnowledgeMapConnectionCatalog>();
}

public sealed class LearningKnowledgeMapNodeCatalog
{
    public string Id { get; set; } = string.Empty;
    public string Cluster { get; set; } = string.Empty;
    public Dictionary<string, string> Title { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    public int Order { get; set; }
    public int X { get; set; }
    public int Y { get; set; }
    public IReadOnlyList<Guid> SessionIds { get; set; } = Array.Empty<Guid>();
}

public sealed class LearningKnowledgeMapConnectionCatalog
{
    public string From { get; set; } = string.Empty;
    public string To { get; set; } = string.Empty;
    public Dictionary<string, string> Label { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}

public sealed class LearningKnowledgeMapResponse
{
    public string Language { get; set; } = "en";
    public IReadOnlyList<LearningKnowledgeMapNode> Nodes { get; set; } = Array.Empty<LearningKnowledgeMapNode>();
    public IReadOnlyList<LearningKnowledgeMapConnection> Connections { get; set; } = Array.Empty<LearningKnowledgeMapConnection>();
}

public sealed class LearningKnowledgeMapNode
{
    public string Id { get; set; } = string.Empty;
    public string Cluster { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public int Order { get; set; }
    public int X { get; set; }
    public int Y { get; set; }
    public IReadOnlyList<Guid> SessionIds { get; set; } = Array.Empty<Guid>();
}

public sealed class LearningKnowledgeMapConnection
{
    public string From { get; set; } = string.Empty;
    public string To { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
}

public sealed class LearningGlossaryEntry
{
    public string Key { get; set; } = string.Empty;
    public Dictionary<string, string> Term { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    public string? Transliteration { get; set; }
    public Dictionary<string, string> Definition { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}

public sealed class LearningSession
{
    public string SchemaVersion { get; set; } = string.Empty;
    public Guid Id { get; set; }
    public string LevelCode { get; set; } = string.Empty;
    public int SessionNumber { get; set; }
    public string Slug { get; set; } = string.Empty;
    public string Language { get; set; } = "en";
    public string Title { get; set; } = string.Empty;
    public string? Subtitle { get; set; }
    public string Summary { get; set; } = string.Empty;
    public int EstimatedMinutes { get; set; }
    public string Difficulty { get; set; } = string.Empty;
    public IReadOnlyList<string> LearningObjectives { get; set; } = Array.Empty<string>();
    public IReadOnlyList<string> Prerequisites { get; set; } = Array.Empty<string>();
    public IReadOnlyList<LearningKeyTerm> KeyTerms { get; set; } = Array.Empty<LearningKeyTerm>();
    public IReadOnlyList<LearningTocItem> TableOfContents { get; set; } = Array.Empty<LearningTocItem>();
    public IReadOnlyList<LearningSection> Sections { get; set; } = Array.Empty<LearningSection>();
    public IReadOnlyList<LearningCommonMistake> CommonMistakes { get; set; } = Array.Empty<LearningCommonMistake>();
    public LearningPracticalExercise? PracticalExercise { get; set; }
    public IReadOnlyList<string> SessionSummary { get; set; } = Array.Empty<string>();
    public LearningSelfCheck? SelfCheck { get; set; }
    public IReadOnlyList<LearningReference> References { get; set; } = Array.Empty<LearningReference>();
    public IReadOnlyList<LearningShlokaReference> ShlokaReferences { get; set; } = Array.Empty<LearningShlokaReference>();
    public IReadOnlyList<LearningFurtherReading> FurtherReading { get; set; } = Array.Empty<LearningFurtherReading>();
    public LearningContentReview? ContentReview { get; set; }
    public string Status { get; set; } = string.Empty;
}

public sealed class LearningKeyTerm
{
    public string Term { get; set; } = string.Empty;
    public string? Sanskrit { get; set; }
    public string? Transliteration { get; set; }
    public string Meaning { get; set; } = string.Empty;
    public string? Note { get; set; }
}

public sealed class LearningTocItem
{
    public string SectionId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public int EstimatedMinutes { get; set; }
}

public sealed class LearningSection
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public int EstimatedMinutes { get; set; }
    public IReadOnlyList<LearningContentBlock> Blocks { get; set; } = Array.Empty<LearningContentBlock>();
}

[JsonConverter(typeof(LearningContentBlockConverter))]
public abstract class LearningContentBlock
{
    public string Type { get; set; } = string.Empty;
}

public sealed class HeadingBlock : LearningContentBlock
{
    public int Level { get; set; }
    public string Text { get; set; } = string.Empty;
}

public sealed class ParagraphBlock : LearningContentBlock
{
    public string Text { get; set; } = string.Empty;
}

public sealed class ListBlock : LearningContentBlock
{
    public string Style { get; set; } = "bullet";
    public IReadOnlyList<string> Items { get; set; } = Array.Empty<string>();
}

public sealed class DefinitionBlock : LearningContentBlock
{
    public string Term { get; set; } = string.Empty;
    public string? Sanskrit { get; set; }
    public string? Transliteration { get; set; }
    public string Meaning { get; set; } = string.Empty;
    public string? Context { get; set; }
}

public sealed class FormulaBlock : LearningContentBlock
{
    public string Expression { get; set; } = string.Empty;
    public string Explanation { get; set; } = string.Empty;
    public string? WorkedExample { get; set; }
}

public sealed class DiagramBlock : LearningContentBlock
{
    public string AssetId { get; set; } = string.Empty;
    public string? AssetPath { get; set; }
    public string AltText { get; set; } = string.Empty;
    public string Caption { get; set; } = string.Empty;
    public string Purpose { get; set; } = string.Empty;
    public IReadOnlyList<string> Annotations { get; set; } = Array.Empty<string>();
}

public sealed class TableBlock : LearningContentBlock
{
    public string Caption { get; set; } = string.Empty;
    public IReadOnlyList<string> Columns { get; set; } = Array.Empty<string>();
    public IReadOnlyList<IReadOnlyList<JsonElement>> Rows { get; set; } = Array.Empty<IReadOnlyList<JsonElement>>();
    public string? Note { get; set; }
}

public sealed class ExampleBlock : LearningContentBlock
{
    public string Title { get; set; } = string.Empty;
    public string Scenario { get; set; } = string.Empty;
    public IReadOnlyList<string> Steps { get; set; } = Array.Empty<string>();
    public string Conclusion { get; set; } = string.Empty;
}

public sealed class NoteBlock : LearningContentBlock
{
    public string Style { get; set; } = "remember";
    public string Title { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
}

public sealed class ComparisonBlock : LearningContentBlock
{
    public string Title { get; set; } = string.Empty;
    public ComparisonSide Left { get; set; } = new();
    public ComparisonSide Right { get; set; } = new();
}

public sealed class ComparisonSide
{
    public string Label { get; set; } = string.Empty;
    public IReadOnlyList<string> Points { get; set; } = Array.Empty<string>();
}

public sealed class DividerBlock : LearningContentBlock
{
}

public sealed class ShlokaTriggerBlock : LearningContentBlock
{
    public string ShlokaId { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public string? Icon { get; set; }
    public string DisplayMode { get; set; } = "collapsed";
}

public sealed class LearningContentBlockConverter : JsonConverter<LearningContentBlock>
{
    private static readonly Dictionary<string, Type> BlockTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        ["heading"] = typeof(HeadingBlock),
        ["paragraph"] = typeof(ParagraphBlock),
        ["list"] = typeof(ListBlock),
        ["definition"] = typeof(DefinitionBlock),
        ["formula"] = typeof(FormulaBlock),
        ["diagram"] = typeof(DiagramBlock),
        ["table"] = typeof(TableBlock),
        ["example"] = typeof(ExampleBlock),
        ["note"] = typeof(NoteBlock),
        ["comparison"] = typeof(ComparisonBlock),
        ["divider"] = typeof(DividerBlock),
        ["shloka-trigger"] = typeof(ShlokaTriggerBlock)
    };

    public override LearningContentBlock Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        using var document = JsonDocument.ParseValue(ref reader);
        if (!document.RootElement.TryGetProperty("type", out var typeProperty))
        {
            throw new JsonException("Learning content block is missing a type property.");
        }

        var blockType = typeProperty.GetString();
        if (string.IsNullOrWhiteSpace(blockType) || !BlockTypes.TryGetValue(blockType, out var targetType))
        {
            throw new JsonException($"Unsupported learning content block type '{blockType}'.");
        }

        var json = document.RootElement.GetRawText();
        var block = (LearningContentBlock?)JsonSerializer.Deserialize(json, targetType, options)
            ?? throw new JsonException($"Unable to deserialize learning content block type '{blockType}'.");
        return WithType(block, blockType);
    }

    public override void Write(Utf8JsonWriter writer, LearningContentBlock value, JsonSerializerOptions options)
    {
        JsonSerializer.Serialize(writer, (object)value, value.GetType(), options);
    }

    private static LearningContentBlock WithType(LearningContentBlock block, string type)
    {
        typeof(LearningContentBlock).GetProperty(nameof(LearningContentBlock.Type))?.SetValue(block, type);
        return block;
    }
}

public sealed class LearningCommonMistake
{
    public string Mistake { get; set; } = string.Empty;
    public string WhyItIsWrong { get; set; } = string.Empty;
    public string CorrectUnderstanding { get; set; } = string.Empty;
}

public sealed class LearningPracticalExercise
{
    public string Title { get; set; } = string.Empty;
    public IReadOnlyList<string> Instructions { get; set; } = Array.Empty<string>();
    public string? ExpectedOutcome { get; set; }
    public int EstimatedMinutes { get; set; }
}

public sealed class LearningSelfCheck
{
    public IReadOnlyList<LearningQuestion> Questions { get; set; } = Array.Empty<LearningQuestion>();
    public int PassingScorePercent { get; set; }
}

public sealed class LearningQuestion
{
    public string Id { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Question { get; set; } = string.Empty;
    public IReadOnlyList<string> Options { get; set; } = Array.Empty<string>();
    public JsonElement Answer { get; set; }
    public string Explanation { get; set; } = string.Empty;
}

public sealed class LearningReference
{
    public string SourceId { get; set; } = string.Empty;
    public string? EditionId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Author { get; set; }
    public string? ChapterTitle { get; set; }
    public string? ChapterNumberAsPrinted { get; set; }
    public string? VerseNumberAsPrinted { get; set; }
    public string? PageNumber { get; set; }
    public string Supports { get; set; } = string.Empty;
    public string? QuotationType { get; set; }
    public string? VerificationStatus { get; set; }
    public string? VerifiedOn { get; set; }
    public string? VerifiedBy { get; set; }
    public string? Url { get; set; }
    public string? Notes { get; set; }
}

public sealed class LearningShlokaReference
{
    public string Id { get; set; } = string.Empty;
    public string? SourceId { get; set; }
    public string? Text { get; set; }
    public string? Transliteration { get; set; }
    public string? Translation { get; set; }
    public string? Context { get; set; }
}

public sealed class LearningFurtherReading
{
    public string Title { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string? Author { get; set; }
    public string? Note { get; set; }
    public string? Url { get; set; }
}

public sealed class LearningContentReview
{
    public bool AuthenticityChecked { get; set; }
    public bool ReferencesChecked { get; set; }
    public bool LanguageReviewed { get; set; }
    public string? ReviewedBy { get; set; }
    public string? ReviewedOn { get; set; }
    public string? ReviewNotes { get; set; }
}





