using System.Text.Json;

namespace KundaliEngine.API.Services.Practice;

public static class PracticeProfileCatalog
{
    public const int NoteMaxLength = 400;

    private static readonly PracticeGroup[] Groups =
    [
        new("astrology", "Astrology",
        [
            new("kundali", "Kundali"),
            new("matching", "Matching"),
            new("muhurta", "Muhurta"),
            new("panchang", "Panchang"),
            new("dasha", "Dasha"),
            new("prashna", "Prashna"),
            new("remedies", "Remedies"),
            new("numerology", "Numerology"),
            new("kp", "KP"),
            new("lal_kitab", "Lal Kitab"),
        ]),
        new("vastu", "Vastu",
        [
            new("vastu_home", "Home"),
            new("vastu_plot", "Plot"),
            new("vastu_office", "Office"),
            new("vastu_factory", "Factory"),
        ]),
        new("karmakand", "Karmakand",
        [
            new("vivah", "Vivah"),
            new("namkaran", "Namkaran"),
            new("annaprashan", "Annaprashan"),
            new("mundan", "Mundan"),
            new("upanayana", "Upanayana"),
            new("griha_pravesh", "Griha pravesh"),
            new("antyeshti", "Antyeshti"),
            new("satyanarayan", "Satyanarayan"),
        ]),
        new("dosha_shanti", "Dosha shanti",
        [
            new("manglik", "Manglik"),
            new("kaal_sarpa", "Kaal Sarpa"),
            new("chandra_grahan", "Chandra Grahan"),
            new("surya_grahan", "Surya Grahan"),
            new("guru_chandal", "Guru Chandal"),
            new("pitra", "Pitra"),
            new("shani", "Shani"),
            new("kemadruma", "Kemadruma"),
            new("daridra", "Daridra"),
            new("navagraha", "Navagraha"),
        ]),
        new("other", "Other",
        [
            new("online", "Online"),
            new("home_visit", "Home visit"),
            new("teaching", "Teaching"),
        ]),
    ];

    private static readonly HashSet<string> KnownKeys = Groups
        .SelectMany(group => group.Items)
        .Select(item => item.Key)
        .ToHashSet(StringComparer.Ordinal);

    public static IReadOnlyList<PracticeGroup> List() => Groups;

    public static IReadOnlyList<string> Normalize(IEnumerable<string>? services)
    {
        return (services ?? [])
            .Select(item => item.Trim())
            .Where(item => KnownKeys.Contains(item))
            .Distinct(StringComparer.Ordinal)
            .ToArray();
    }

    public static string Serialize(IReadOnlyList<string> services, string? note)
    {
        return JsonSerializer.Serialize(new StoredPracticeProfile
        {
            Services = services.ToArray(),
            Note = string.IsNullOrWhiteSpace(note) ? null : note.Trim(),
        });
    }

    public static StoredPracticeProfile Read(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return new StoredPracticeProfile();
        }

        try
        {
            var stored = JsonSerializer.Deserialize<StoredPracticeProfile>(json) ?? new StoredPracticeProfile();
            return new StoredPracticeProfile
            {
                Services = Normalize(stored.Services).ToArray(),
                Note = stored.Note,
            };
        }
        catch (JsonException)
        {
            return new StoredPracticeProfile();
        }
    }
}

public sealed record PracticeGroup(string Key, string Title, PracticeItem[] Items);

public sealed record PracticeItem(string Key, string Label);

public sealed class StoredPracticeProfile
{
    public string[] Services { get; set; } = [];

    public string? Note { get; set; }
}
