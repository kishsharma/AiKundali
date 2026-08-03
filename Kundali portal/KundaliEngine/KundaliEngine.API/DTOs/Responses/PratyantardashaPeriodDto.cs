using System;

namespace KundaliEngine.API.DTOs.Responses
{
    public class PratyantardashaPeriodDto
    {
        public string InnerPlanet { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
    }
}
