namespace KundaliEngine.API.DTOs.Requests
{
    public class GenerateKundaliRequest
    {
        public string Name { get; set; }

        public string Gender { get; set; }

        public DateTime BirthDateTime { get; set; }

        public string BirthPlace { get; set; }

        public double Latitude { get; set; }

        public double Longitude { get; set; }
        public double TimeZoneOffset { get; internal set; }
    }
}
