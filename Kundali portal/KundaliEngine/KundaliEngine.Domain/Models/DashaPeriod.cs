using System;
using System.Collections.Generic;
using KundaliEngine.Domain.Enums;

namespace KundaliEngine.Domain.Models
{
    public class DashaPeriod
    {
        public Planet Lord { get; set; }
        public DateTime StartDateUtc { get; set; }
        public DateTime EndDateUtc { get; set; }
        public List<DashaPeriod> SubPeriods { get; set; } = new List<DashaPeriod>();
    }
}
