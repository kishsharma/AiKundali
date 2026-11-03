namespace KundaliEngine.Domain.Entities;

public sealed class PractitionerClient
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string? Surname { get; set; }
    public string? FatherName { get; set; }
    public string? MotherName { get; set; }
    public string? Gotra { get; set; }
    public string Gender { get; set; } = string.Empty;
    public DateOnly DateOfBirth { get; set; }
    public TimeOnly TimeOfBirth { get; set; }
    public string BirthPlace { get; set; } = string.Empty;
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
    public string? PhoneNumber { get; set; }
    public string? Email { get; set; }
    public string? Topic { get; set; }
    public string Status { get; set; } = "New";
    public string? Notes { get; set; }
    public bool IsArchived { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public User? User { get; set; }
    public ICollection<Kundali> Kundalis { get; set; } = new List<Kundali>();
    public ICollection<PractitionerAppointment> Appointments { get; set; } = new List<PractitionerAppointment>();
}
