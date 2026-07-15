namespace KundaliEngine.API.DTOs.Responses
{
    public class AstrologerBrandingSettingDto
    {
        public Guid Id { get; set; }

        public Guid UserId { get; set; }

        public string AstrologerName { get; set; } = string.Empty;

        public string? Designation { get; set; }

        public string? Address { get; set; }

        public string? ContactNumber { get; set; }

        public string? Email { get; set; }

        public string? LogoUrl { get; set; }

        public bool ShowOnChartPage { get; set; }

        public bool ShowOnCompatibilityPage { get; set; }

        public bool ShowOnPdfReport { get; set; }

        public bool IsActive { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime? UpdatedAt { get; set; }
    }
}
