using System.Text.Json;
using System.Text.Json.Serialization;
using HoraLife.PredictionEngine.Enums;

namespace HoraLife.PredictionEngine.Metadata;

internal sealed class PredictionMetadataProvider
{
    private readonly IReadOnlyDictionary<PredictionLifeArea, PredictionLifeAreaMetadata> _lifeAreas;

    private readonly PredictionMetadataRoot _root;
    private readonly HouseMetadataRoot _houseRoot;


    public PredictionMetadataProvider(string metadataPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(metadataPath);


        if (!File.Exists(metadataPath))
        {
            throw new FileNotFoundException(
                "Prediction metadata file was not found.",
                metadataPath);
        }
        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        options.Converters.Add(new JsonStringEnumConverter());

        var json = File.ReadAllText(metadataPath);

        _root = JsonSerializer.Deserialize<PredictionMetadataRoot>(
            json,
            options)
            ?? throw new InvalidOperationException(
                "Prediction metadata could not be deserialized.");

        Validate(_root);

        _lifeAreas = _root.LifeAreas.ToDictionary(area => area.LifeArea);


        var metadataDirectory =
            Path.GetDirectoryName(metadataPath)
            ?? throw new InvalidOperationException(
                $"Unable to resolve metadata directory from '{metadataPath}'.");

        var houseMetadataPath = Path.Combine(metadataDirectory,"house-significations.json");

        if (!File.Exists(houseMetadataPath))
        {
            throw new FileNotFoundException(
                "House signification metadata file was not found.",
                houseMetadataPath);
        }

        var houseJson = File.ReadAllText(houseMetadataPath);

        _houseRoot = JsonSerializer.Deserialize<HouseMetadataRoot>(
            houseJson,
            options)
            ?? throw new InvalidOperationException(
                "House signification metadata could not be deserialized.");

        ValidateHouseMetadata(_houseRoot);
    }

    public PredictionLifeAreaMetadata GetLifeArea(PredictionLifeArea lifeArea)
    {
        return _lifeAreas.TryGetValue(lifeArea, out var metadata)
            ? metadata  : throw new NotSupportedException($"Prediction metadata for '{lifeArea}' is not defined.");
    }

    private static void Validate(PredictionMetadataRoot root)
    {
        if (root.LifeAreas.Count == 0)
        {
            throw new InvalidOperationException("Prediction metadata must contain at least one life area.");
        }

        foreach (var area in root.LifeAreas)
        {
            if (area.PrimaryHouses.Count == 0)
            {
                throw new InvalidOperationException($"'{area.LifeArea}' must define at least one primary house.");
            }

            if (area.PrimaryHouses.Concat(area.SupportingHouses).Any(house => house is < 1 or > 12))
            {
                throw new InvalidOperationException($"'{area.LifeArea}' contains an invalid house number.");
            }
        }

        var duplicate = root.LifeAreas.GroupBy(area => area.LifeArea).FirstOrDefault(group => group.Count() > 1);

        if (duplicate is not null)
        {
            throw new InvalidOperationException($"Duplicate Prediction life area '{duplicate.Key}'.");
        }
    }

    public HouseSignificationMetadata GetHouse(int houseNumber)
    {
        if (houseNumber is < 1 or > 12)
        {
            throw new ArgumentOutOfRangeException(
                nameof(houseNumber),
                houseNumber,
                "House number must be between 1 and 12.");
        }

        return _houseRoot.Houses
            .FirstOrDefault(house => house.HouseNumber == houseNumber)
            ?? throw new KeyNotFoundException(
                $"House metadata was not found for house {houseNumber}.");
    }

    private static void ValidateHouseMetadata(HouseMetadataRoot root)
    {
        if (root.Houses.Count != 12)
        {
            throw new InvalidOperationException(
                $"House metadata must contain exactly 12 houses. Found {root.Houses.Count}.");
        }

        var duplicateHouse = root.Houses
            .GroupBy(house => house.HouseNumber)
            .FirstOrDefault(group => group.Count() > 1);

        if (duplicateHouse is not null)
        {
            throw new InvalidOperationException(
                $"Duplicate house metadata found for house {duplicateHouse.Key}.");
        }

        var expectedHouses = Enumerable.Range(1, 12).ToHashSet();
        var actualHouses = root.Houses
            .Select(house => house.HouseNumber)
            .ToHashSet();

        if (!expectedHouses.SetEquals(actualHouses))
        {
            throw new InvalidOperationException(
                "House metadata must define every house from 1 through 12 exactly once.");
        }

        var validClassifications = root.ClassificationDefinitions
            .Select(item => item.Key)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var validPurusharthas = root.PurusharthaDefinitions
            .Select(item => item.Key)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var house in root.Houses)
        {
            if (string.IsNullOrWhiteSpace(house.Key) ||
                string.IsNullOrWhiteSpace(house.CanonicalName))
            {
                throw new InvalidOperationException(
                    $"House {house.HouseNumber} has incomplete identity metadata.");
            }

            if (!validPurusharthas.Contains(house.Purushartha))
            {
                throw new InvalidOperationException(
                    $"House {house.HouseNumber} references unknown Purushartha '{house.Purushartha}'.");
            }

            foreach (var classification in house.Classifications)
            {
                if (!validClassifications.Contains(classification))
                {
                    throw new InvalidOperationException(
                        $"House {house.HouseNumber} references unknown classification '{classification}'.");
                }
            }
        }
    }
}