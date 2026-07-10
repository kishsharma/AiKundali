using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KundaliEngine.Domain.Models
{
    public class PanchangDetails
    {
        public string Tithi { get; set; } = string.Empty;
        public string Paksha { get; set; } = string.Empty;
        public string Nakshatra { get; set; } = string.Empty;
        public int Pada { get; set; }
        public string Akshara { get; set; } = string.Empty;
        public string Yoga { get; set; } = string.Empty;
        public string Karana { get; set; } = string.Empty;
        public string Vaar { get; set; } = string.Empty;
        public string Maas { get; set; } = string.Empty;
        public string Shake { get; set; } = string.Empty;   // <-- Added
        public string Samvat { get; set; } = string.Empty;  // <-- Added
    }
}
