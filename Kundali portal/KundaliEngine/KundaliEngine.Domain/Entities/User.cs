using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KundaliEngine.Domain.Entities
{
    public class User
    {
        public Guid Id { get; set; }

        public string FullName { get; set; } = string.Empty;

        public string? Email { get; set; }

        public string? PhoneNumber { get; set; }

        public string? PreferredLanguage { get; set; }

        public string? PracticeProfile { get; set; }

        public string? PasswordHash { get; set; }

        public string AuthProvider { get; set; } = "Local";

        public string Role { get; set; } = "User";

        public string PlanCode { get; set; } = "Free";

        public DateTimeOffset? PlanExpiresAt { get; set; }

        public bool IsActive { get; set; } = true;

        public int KundaliSaveLimit { get; set; } = 5;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        public DateTime? LastLoginAt { get; set; }

        public DateTime? EmailVerifiedAt { get; set; }

        public DateTime? PhoneVerifiedAt { get; set; }
    }
}


