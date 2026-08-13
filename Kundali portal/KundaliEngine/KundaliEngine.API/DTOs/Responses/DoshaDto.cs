namespace KundaliEngine.API.DTOs.Responses
{
    public class DoshaDto
    {
        public string Key { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string Severity { get; set; } = "Low";
        public bool IsPresent { get; set; }
        public string Description { get; set; } = string.Empty;
        public string Reason { get; set; } = string.Empty;
        public List<string> InvolvedPlanets { get; set; } = new();
        public List<int> InvolvedHouses { get; set; } = new();
        public List<DoshaImpactAreaDto> ImpactAreas { get; set; } = new();
        public List<string> Remedies { get; set; } = new();
        public List<string> References { get; set; } = new();
        public FormationActivationPeriodsDto? ActivationPeriods { get; set; }
    }

    public class DoshaImpactAreaDto
    {
        public string Category { get; set; } = string.Empty;
        public List<string> Statements { get; set; } = new();
    }
}

