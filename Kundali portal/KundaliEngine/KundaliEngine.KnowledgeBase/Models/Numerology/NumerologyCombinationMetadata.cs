using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KundaliEngine.KnowledgeBase.Models.Numerology
{
    public sealed class NumerologyCombinationMetadata
    {
        public int Mulank { get; set; }

        public int Bhagyank { get; set; }

        public string Type { get; set; } = string.Empty;

        public string Summary { get; set; } = string.Empty;

        public List<string> Strengths { get; set; } = [];

        public List<string> Challenges { get; set; } = [];
    }
}
