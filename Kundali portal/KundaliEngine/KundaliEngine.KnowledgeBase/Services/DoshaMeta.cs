using System.Collections.Generic;

namespace KundaliEngine.Domain.Models
{
    public class DoshaMeta
    {
        public string Key { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string Nature { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public List<DoshaImpactArea> ImpactAreas { get; set; } = new();
        public List<string> Remedies { get; set; } = new();
        public List<string> References { get; set; } = new();
    }

    public class DoshaImpactArea
    {
        public string Category { get; set; } = string.Empty;
        public List<string> Statements { get; set; } = new();
    }
}
