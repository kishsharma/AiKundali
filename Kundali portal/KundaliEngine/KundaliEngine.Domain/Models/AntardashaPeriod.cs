using KundaliEngine.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KundaliEngine.Domain.Models
{
    public class AntardashaPeriod
    {
        public Planet MainPlanet { get; set; }
        public Planet SubPlanet { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
    }
}
