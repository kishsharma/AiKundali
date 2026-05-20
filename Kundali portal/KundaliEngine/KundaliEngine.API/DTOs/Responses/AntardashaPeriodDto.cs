using System;

namespace KundaliEngine.API.DTOs.Responses
{
    public class AntardashaPeriodDto
    {
        public string SubPlanet { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
    }
}
