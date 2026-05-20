using System;
using System.Collections.Generic;

namespace KundaliEngine.API.DTOs.Responses
{
    public class MahadashaPeriodDto
    {
        public string Planet { get; set; } = string.Empty;
        public DateTime Start { get; set; }
        public DateTime End { get; set; }
        public List<AntardashaPeriodDto> Antardashas { get; set; } = new List<AntardashaPeriodDto>();
    }
}
