using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KundaliEngine.KnowledgeBase.Models.Numerology
{
    public sealed class NumerologyCombinationMetadataFile
    {
        public string Version { get; set; } = string.Empty;

        public string System { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        public List<NumerologyCombinationMetadata> Combinations { get; set; } = new List<NumerologyCombinationMetadata>();
    }
}
