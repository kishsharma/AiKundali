namespace KundaliEngine.API.DTOs.Muhurtha.Requests;

public class MuhurthaHoroscopeReferenceDto
{
    public string? Name { get; set; }

    public DateTime? BirthDateTime { get; set; }

    public string? BirthPlace { get; set; }

    public double? Latitude { get; set; }

    public double? Longitude { get; set; }

    public string? Timezone { get; set; }
}
