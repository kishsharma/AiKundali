namespace KundaliEngine.API.DTOs.Responses
{
    public class CurrentDashaDto
    {
        public string Mahadasha { get; set; }

        public string Antardasha { get; set; }

        public DateTime StartDate { get; set; }

        public DateTime EndDate { get; set; }

        public DateTime AntardashaStartDate { get; set; }

        public DateTime AntardashaEndDate { get; set; }

        public string Summary { get; set; }
    }
}
