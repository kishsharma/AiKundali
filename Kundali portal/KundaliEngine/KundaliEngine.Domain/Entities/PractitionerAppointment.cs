namespace KundaliEngine.Domain.Entities;

public sealed class PractitionerAppointment
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid ClientId { get; set; }
    public Guid? KundaliId { get; set; }
    public DateTime StartsAtUtc { get; set; }
    public DateTime EndsAtUtc { get; set; }
    public string TimeZoneId { get; set; } = "Asia/Kolkata";
    public string Status { get; set; } = "Consultation Pending";
    public string? Topic { get; set; }
    public DateTime? CompletedAtUtc { get; set; }
    public int? ReminderLeadMinutes { get; set; } = 60;
    public int ReminderVersion { get; set; } = 1;
    public int? ReminderSentVersion { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public PractitionerClient? Client { get; set; }
    public Kundali? Kundali { get; set; }
}
