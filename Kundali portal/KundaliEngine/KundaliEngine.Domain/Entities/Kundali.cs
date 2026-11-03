namespace KundaliEngine.Domain.Entities;

public class Kundali
{
    public Guid Id { get; set; }

    public Guid? UserId { get; set; }

    public Guid? ClientId { get; set; }

    public string FullName { get; set; } = string.Empty;

    public string? Surname { get; set; }

    public string? FatherName { get; set; }

    public string? MotherName { get; set; }

    public string? Gotra { get; set; }

    public string? Gender { get; set; }

    public DateOnly DateOfBirth { get; set; }

    public TimeOnly TimeOfBirth { get; set; }

    public string BirthPlace { get; set; } = string.Empty;

    public decimal? Latitude { get; set; }

    public decimal? Longitude { get; set; }

    public string? TimeZone { get; set; }

    public string ChartDataJson { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }

    public User? User { get; set; }

    public PractitionerClient? Client { get; set; }

    public ICollection<KundaliReport> Reports { get; set; } = new List<KundaliReport>();
}
