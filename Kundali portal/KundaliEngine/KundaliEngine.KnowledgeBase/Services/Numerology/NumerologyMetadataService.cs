using KundaliEngine.KnowledgeBase.Models.Numerology;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace KundaliEngine.KnowledgeBase.Services.Numerology
{
    public sealed class NumerologyMetadataService
    {
        private readonly NumerologyCombinationMetadataFile _metadata;
        private readonly NumerologyNumberMetadataFile _numberMetadata;
        public NumerologyMetadataService()
        {
            var path = Path.Combine(AppContext.BaseDirectory, "Data", "Numerology", "numerology-combinations.json");

            if (!File.Exists(path))
            {
                throw new FileNotFoundException(
                    "Numerology combination metadata file was not found.",
                    path);
            }

            var json = File.ReadAllText(path);

            _metadata =JsonSerializer.Deserialize<NumerologyCombinationMetadataFile>(json,
                    new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    })
                ?? throw new InvalidOperationException("Unable to deserialize numerology combination metadata.");

            ValidateMetadata();


            var numbersPath = Path.Combine(AppContext.BaseDirectory, "Data", "Numerology",  "numerology-numbers.json");

            if (!File.Exists(numbersPath))
            {
                throw new FileNotFoundException(
                    "Numerology number metadata file was not found.",
                    numbersPath);
            }

            var numbersJson = File.ReadAllText(numbersPath);

            _numberMetadata =
                JsonSerializer.Deserialize<NumerologyNumberMetadataFile>(
                    numbersJson,
                    new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    })
                ?? throw new InvalidOperationException(
                    "Unable to deserialize numerology number metadata.");

        }

        public NumerologyCombinationMetadata? GetCombination(int mulank, int bhagyank)
        {
            if (mulank is < 1 or > 9)
                throw new ArgumentOutOfRangeException(nameof(mulank));

            if (bhagyank is < 1 or > 9)
                throw new ArgumentOutOfRangeException(nameof(bhagyank));

            return _metadata.Combinations
                .FirstOrDefault(x =>
                    x.Mulank == mulank &&
                    x.Bhagyank == bhagyank);
        }

        private void ValidateMetadata()
        {
            if (_metadata.Combinations.Count != 81)
            {
                throw new InvalidOperationException(
                    $"Numerology combinations must contain exactly 81 entries. Found {_metadata.Combinations.Count}.");
            }

            var duplicates = _metadata.Combinations
                .GroupBy(x => new { x.Mulank, x.Bhagyank })
                .Where(g => g.Count() > 1)
                .ToList();

            if (duplicates.Count > 0)
            {
                throw new InvalidOperationException(
                    "Duplicate Mulank/Bhagyank combinations found in numerology metadata.");
            }

            for (int mulank = 1; mulank <= 9; mulank++)
            {
                for (int bhagyank = 1; bhagyank <= 9; bhagyank++)
                {
                    bool exists = _metadata.Combinations.Any(x =>
                        x.Mulank == mulank &&
                        x.Bhagyank == bhagyank);

                    if (!exists)
                    {
                        throw new InvalidOperationException(
                            $"Missing numerology combination: {mulank} + {bhagyank}.");
                    }
                }
            }
        }

        public NumerologyNumberMetadata? GetNumber(int number)
        {
            if (number is < 1 or > 9)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(number),
                    "Numerology number must be between 1 and 9.");
            }

            return _numberMetadata.Numbers
                .FirstOrDefault(x => x.Number == number);
        }
    }

}