namespace KundaliEngine.API.DTOs.Responses.Numerology
{
    public class NumerologyCalculator
    {

        public NumerologyResponse Calculate(DateOnly dateOfBirth)
        {
            int mulank = CalculateMulank(dateOfBirth);
            int bhagyank = CalculateBhagyank(dateOfBirth);

            return new NumerologyResponse
            {
                DateOfBirth = dateOfBirth,

                Mulank = mulank,
                Bhagyank = bhagyank,

                MulankPlanet = GetRulingPlanet(mulank),
                BhagyankPlanet = GetRulingPlanet(bhagyank),

                MulankCalculation = BuildMulankCalculation(dateOfBirth),
                BhagyankCalculation = BuildBhagyankCalculation(dateOfBirth)
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
}
