namespace KundaliEngine.API.DTOs.Requests.Numerology
{
    public class NumerologyRequest
    {
        public DateOnly DateOfBirth { get; set; }

        public string? Name { get; set; }

        public string? MobileNumber { get; set; }

        public string? BusinessName { get; set; }
    }
}
