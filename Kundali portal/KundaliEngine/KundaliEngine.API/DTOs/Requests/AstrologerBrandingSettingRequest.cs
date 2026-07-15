namespace KundaliEngine.API.DTOs.Requests
{
    public class AstrologerBrandingSettingRequest
    {
        public string AstrologerName { get; set; } = string.Empty;

        public string? Designation { get; set; }

        public string? Address { get; set; }

        public string? ContactNumber { get; set; }

        public string? Email { get; set; }

        public string? LogoUrl { get; set; }

        public bool ShowOnChartPage { get; set; } = true;

        public bool ShowOnCompatibilityPage { get; set; } = true;

        public bool ShowOnPdfReport { get; set; } = true;

        public bool IsActive { get; set; } = true;
    }
}
