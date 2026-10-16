namespace KundaliEngine.API.DTOs.Responses.Numerology
{
    public class NumerologyCalculator
    {

        public NumerologyResponse Calculate(DateOnly dateOfBirth, int? calendarYear = null)
        {
            int mulank = CalculateMulank(dateOfBirth);
            int bhagyank = CalculateBhagyank(dateOfBirth);
            int year = calendarYear ?? DateTime.UtcNow.Year;

            return new NumerologyResponse
            {
                DateOfBirth = dateOfBirth,

                Mulank = mulank,
                Bhagyank = bhagyank,

                MulankPlanet = GetRulingPlanet(mulank),
                BhagyankPlanet = GetRulingPlanet(bhagyank),

                MulankCalculation = BuildMulankCalculation(dateOfBirth),
                BhagyankCalculation = BuildBhagyankCalculation(dateOfBirth),
                LoShu = BuildLoShu(dateOfBirth),
                PersonalYear = BuildPersonalYear(dateOfBirth, year),
                NextPersonalYear = BuildPersonalYear(dateOfBirth, year + 1)
            };
        }
        public int CalculateMulank(DateOnly dateOfBirth)
        {
            return ReduceToSingleDigit(dateOfBirth.Day);
        }

        public int CalculateBhagyank(DateOnly dateOfBirth)
        {
            int sum =
                ReduceToSingleDigit(dateOfBirth.Day) +
                ReduceToSingleDigit(dateOfBirth.Month) +
                ReduceToSingleDigit(dateOfBirth.Year);

            return ReduceToSingleDigit(sum);
        }

        public int ReduceToSingleDigit(int number)
        {
            number = Math.Abs(number);

            while (number > 9)
            {
                int sum = 0;

                while (number > 0)
                {
                    sum += number % 10;
                    number /= 10;
                }

                number = sum;
            }

            return number;
        }
        private string BuildMulankCalculation(DateOnly dateOfBirth)
        {
            int day = dateOfBirth.Day;

            if (day <= 9)
                return day.ToString();

            int firstDigit = day / 10;
            int secondDigit = day % 10;
            int result = CalculateMulank(dateOfBirth);

            return $"{firstDigit} + {secondDigit} = {result}";
        }

        private string BuildBhagyankCalculation(DateOnly dateOfBirth)
        {
            string digits = dateOfBirth.ToString("ddMMyyyy");

            var values = digits
                .Select(c => c - '0')
                .ToList();

            int sum = values.Sum();

            string calculation = string.Join(" + ", values) + $" = {sum}";

            int current = sum;

            while (current > 9)
            {
                var currentDigits = current
                    .ToString()
                    .Select(c => c - '0')
                    .ToList();

                int next = currentDigits.Sum();

                calculation += $" → {string.Join(" + ", currentDigits)} = {next}";

                current = next;
            }

            return calculation;
        }
        private static readonly int[] LoShuOrder = [4, 9, 2, 3, 5, 7, 8, 1, 6];

        private static readonly Dictionary<char, int> Chaldean = new()
        {
            ['A'] = 1, ['I'] = 1, ['J'] = 1, ['Q'] = 1, ['Y'] = 1,
            ['B'] = 2, ['K'] = 2, ['R'] = 2,
            ['C'] = 3, ['G'] = 3, ['L'] = 3, ['S'] = 3,
            ['D'] = 4, ['M'] = 4, ['T'] = 4,
            ['E'] = 5, ['H'] = 5, ['N'] = 5, ['X'] = 5,
            ['U'] = 6, ['V'] = 6, ['W'] = 6,
            ['O'] = 7, ['Z'] = 7,
            ['F'] = 8, ['P'] = 8
        };

        private static readonly IReadOnlyDictionary<int, string[]> ChaldeanExamples = new Dictionary<int, string[]>
        {
            [1] = ["A", "I", "J"],
            [2] = ["B", "K", "R"],
            [3] = ["C", "G", "L"],
            [4] = ["D", "M", "T"],
            [5] = ["E", "H", "N"],
            [6] = ["U", "V", "W"],
            [7] = ["O", "Z"],
            [8] = ["F", "P"]
        };

        public bool TryCalculateName(string? rawName, out NumerologyNameValue? value)
        {
            value = null;
            if (string.IsNullOrWhiteSpace(rawName))
            {
                return true;
            }

            var display = string.Join(' ', rawName.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
            var parts = new List<string>();
            var compound = 0;
            var letters = 0;
            foreach (var character in display)
            {
                if (!char.IsLetter(character))
                {
                    continue;
                }

                var upper = char.ToUpperInvariant(character);
                if (upper is < 'A' or > 'Z' || !Chaldean.TryGetValue(upper, out var weight))
                {
                    continue;
                }

                letters++;
                compound += weight;
                parts.Add($"{upper}{weight}");
            }

            if (letters == 0)
            {
                return false;
            }

            var number = ReduceToSingleDigit(compound);
            var calculation = $"{string.Join(" + ", parts)} = {compound}";
            if (compound > 9)
            {
                calculation += $" → {DescribeReduction(compound)}";
            }

            value = new NumerologyNameValue
            {
                Name = display,
                Compound = compound,
                Number = number,
                Planet = GetRulingPlanet(number),
                Calculation = calculation
            };
            return true;
        }

        public bool TryCalculateMobile(string? rawMobile, out NumerologyMobileValue? value)
        {
            value = null;
            if (string.IsNullOrWhiteSpace(rawMobile))
            {
                return true;
            }

            var display = rawMobile.Trim();
            var digits = display.Where(char.IsDigit).Select(character => character - '0').ToList();
            if (digits.Count == 0)
            {
                return false;
            }

            var compound = digits.Sum();
            var number = ReduceToSingleDigit(compound);
            var calculation = $"{string.Join(" + ", digits)} = {compound}";
            if (compound > 9)
            {
                calculation += $" → {DescribeReduction(compound)}";
            }

            value = new NumerologyMobileValue
            {
                Mobile = display,
                Digits = digits,
                Compound = compound,
                Number = number,
                Planet = GetRulingPlanet(number),
                Calculation = calculation
            };
            return true;
        }

        public IReadOnlyList<string> SuggestMobileCorrections(
            NumerologyMobileValue mobile,
            IEnumerable<int> supportiveNumbers)
        {
            var targets = supportiveNumbers
                .Where(number => number is >= 1 and <= 9 && number != mobile.Number)
                .Distinct()
                .OrderBy(number => number)
                .ToList();
            if (targets.Count == 0 || mobile.Digits.Count == 0)
            {
                return [];
            }

            var suggestions = new List<string>();
            var lastIndex = mobile.Digits.Count - 1;
            var lastDigit = mobile.Digits[lastIndex];
            var baseWithoutLast = mobile.Compound - lastDigit;

            foreach (var target in targets)
            {
                for (var digit = 0; digit <= 9; digit++)
                {
                    if (digit == lastDigit)
                    {
                        continue;
                    }

                    if (ReduceToSingleDigit(baseWithoutLast + digit) != target)
                    {
                        continue;
                    }

                    suggestions.Add($"Change the last digit to {digit} so the mobile reduces to {target}.");
                    break;
                }

                if (suggestions.Count >= 4)
                {
                    return suggestions;
                }
            }

            foreach (var target in targets)
            {
                for (var digit = 0; digit <= 9; digit++)
                {
                    if (ReduceToSingleDigit(mobile.Compound + digit) != target)
                    {
                        continue;
                    }

                    suggestions.Add($"Add {digit} at the end so the mobile reduces to {target}.");
                    break;
                }

                if (suggestions.Count >= 4)
                {
                    break;
                }
            }

            return suggestions;
        }

        public IReadOnlyList<string> SuggestNameCorrections(
            NumerologyNameValue name,
            IEnumerable<int> supportiveNumbers)
        {
            var targets = supportiveNumbers
                .Where(number => number is >= 1 and <= 9 && number != name.Number)
                .Distinct()
                .OrderBy(number => number)
                .ToList();
            if (targets.Count == 0)
            {
                return [];
            }

            var suggestions = new List<string>();
            foreach (var target in targets)
            {
                for (var weight = 1; weight <= 8; weight++)
                {
                    if (ReduceToSingleDigit(name.Compound + weight) != target)
                    {
                        continue;
                    }

                    var example = ChaldeanExamples[weight][0];
                    suggestions.Add(
                        $"Add a letter valued {weight} (for example {example}) so \"{name.Name}\" reduces to {target}.");
                    break;
                }

                if (suggestions.Count >= 4)
                {
                    break;
                }
            }

            if (suggestions.Count == 0)
            {
                suggestions.Add(
                    $"Prefer a spelling whose compound reduces to {string.Join(", ", targets)}.");
            }

            return suggestions;
        }

        public static IReadOnlyList<int> SupportiveNumbers(
            int mulank,
            int bhagyank,
            IReadOnlyList<int>? mulankFavourable,
            IReadOnlyList<int>? bhagyankFavourable)
        {
            return new[] { mulank, bhagyank }
                .Concat(mulankFavourable ?? [])
                .Concat(bhagyankFavourable ?? [])
                .Where(number => number is >= 1 and <= 9)
                .Distinct()
                .OrderBy(number => number)
                .ToList();
        }

        private NumerologyLoShuResponse BuildLoShu(DateOnly dateOfBirth)
        {
            var counts = new int[10];
            foreach (var digit in dateOfBirth.ToString("ddMMyyyy").Select(character => character - '0'))
            {
                if (digit is >= 1 and <= 9)
                {
                    counts[digit]++;
                }
            }

            return new NumerologyLoShuResponse
            {
                Cells = LoShuOrder.Select(number => new NumerologyLoShuCellResponse
                {
                    Number = number,
                    Count = counts[number]
                }).ToList(),
                Missing = Enumerable.Range(1, 9).Where(number => counts[number] == 0).ToList(),
                Repeated = Enumerable.Range(1, 9)
                    .Where(number => counts[number] > 1)
                    .Select(number => new NumerologyRepeatResponse { Number = number, Count = counts[number] })
                    .ToList()
            };
        }

        private NumerologyPersonalYearResponse BuildPersonalYear(DateOnly dateOfBirth, int year)
        {
            int day = ReduceToSingleDigit(dateOfBirth.Day);
            int month = ReduceToSingleDigit(dateOfBirth.Month);
            int reducedYear = ReduceToSingleDigit(year);
            int sum = day + month + reducedYear;
            int number = ReduceToSingleDigit(sum);
            string calculation = $"{day} + {month} + {reducedYear} = {sum}";
            if (sum > 9)
            {
                calculation += $" → {DescribeReduction(sum)}";
            }

            return new NumerologyPersonalYearResponse
            {
                Year = year,
                Number = number,
                Planet = GetRulingPlanet(number),
                Calculation = calculation
            };
        }

        private string DescribeReduction(int number)
        {
            var parts = new List<string>();
            var current = number;
            while (current > 9)
            {
                var digits = current.ToString().Select(character => character - '0').ToList();
                var next = digits.Sum();
                parts.Add($"{string.Join(" + ", digits)} = {next}");
                current = next;
            }

            return string.Join(" → ", parts);
        }

        public string GetRulingPlanet(int number)
        {
            return number switch
            {
                1 => "Sun",
                2 => "Moon",
                3 => "Jupiter",
                4 => "Rahu",
                5 => "Mercury",
                6 => "Venus",
                7 => "Ketu",
                8 => "Saturn",
                9 => "Mars",
                _ => throw new ArgumentOutOfRangeException(
                    nameof(number),
                    "Numerology number must be between 1 and 9.")
            };
        }
    }

    public sealed class NumerologyNameValue
    {
        public string Name { get; init; } = string.Empty;

        public int Compound { get; init; }

        public int Number { get; init; }

        public string Planet { get; init; } = string.Empty;

        public string Calculation { get; init; } = string.Empty;
    }

    public sealed class NumerologyMobileValue
    {
        public string Mobile { get; init; } = string.Empty;

        public IReadOnlyList<int> Digits { get; init; } = [];

        public int Compound { get; init; }

        public int Number { get; init; }

        public string Planet { get; init; } = string.Empty;

        public string Calculation { get; init; } = string.Empty;
    }
}
