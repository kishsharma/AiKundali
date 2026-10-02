using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KundaliEngine.KnowledgeBase.Models.Numerology
{
    public sealed class NumerologyNumberMetadata
    {
        public int Number { get; set; }

        public string Planet { get; set; } = string.Empty;

        public string PlanetKey { get; set; } = string.Empty;

        public string Summary { get; set; } = string.Empty;

        public List<string> Traits { get; set; } = [];

        public List<string> Strengths { get; set; } = [];

        public List<string> Challenges { get; set; } = [];

        public List<int> FavourableNumbers { get; set; } = [];

        public List<string> FavourableDays { get; set; } = [];

        public List<string> FavourableColours { get; set; } = [];
    }
}
