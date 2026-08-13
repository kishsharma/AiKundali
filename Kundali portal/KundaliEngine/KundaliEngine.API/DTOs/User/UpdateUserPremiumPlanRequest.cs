namespace KundaliEngine.API.DTOs.User
{
    public class UpdateUserPremiumPlanRequest
    {
        public bool IsPremium { get; set; }

        public DateTimeOffset? PlanExpiresAt { get; set; }
    }
}