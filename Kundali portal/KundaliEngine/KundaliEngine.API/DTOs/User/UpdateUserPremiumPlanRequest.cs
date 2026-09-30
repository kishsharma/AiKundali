namespace KundaliEngine.API.DTOs.User
{
    public class UpdateUserPremiumPlanRequest
    {
        public string? PlanCode { get; set; }

        public bool? IsPremium { get; set; }

        public DateTimeOffset? PlanExpiresAt { get; set; }
    }
}