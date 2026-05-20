namespace KundaliEngine.API.DTOs.Requests
{
    public class GenerateKundaliRequest
    {
        public string Name { get; set; }

        public DateTime BirthDateTime { get; set; }

        public double Latitude { get; set; }

        public double Longitude { get; set; }

        public double TimeZoneOffset { get; set; }
    }
}
