namespace KundaliEngine.Domain.Entities;

public class DhruvaDailyUsage
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public DateOnly UsageDate { get; set; }

    public int LifeQuestionCount { get; set; }
}
