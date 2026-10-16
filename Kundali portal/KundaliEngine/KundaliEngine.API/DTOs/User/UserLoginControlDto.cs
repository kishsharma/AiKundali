namespace KundaliEngine.API.DTOs.User
{
    public class UserLoginControlDto
    {
        public Guid Id { get; set; }

        public string FullName { get; set; } = string.Empty;

        public string? Email { get; set; }

        public string? PhoneNumber { get; set; }

        public bool IsActive { get; set; }

        public int KundaliSaveLimit { get; set; }

        public string PlanCode { get; set; } = "Free";

        public DateTimeOffset? PlanExpiresAt { get; set; }

        public bool IsPremium { get; set; }

        public bool IsPaid { get; set; }

        public bool IsPlus { get; set; }

        public int MatchSaveLimit { get; set; }

        public DateTime? LastSeenAt { get; set; }

        public bool IsOnline { get; set; }
    }
}