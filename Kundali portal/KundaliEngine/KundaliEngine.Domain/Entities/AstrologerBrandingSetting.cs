namespace KundaliEngine.Domain.Entities
{
    public class AstrologerBrandingSetting
    {
        public Guid Id { get; set; }

        public Guid UserId { get; set; }

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

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        public User? User { get; set; }
    }
}
