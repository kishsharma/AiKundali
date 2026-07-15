namespace KundaliEngine.Domain.Models.Muhurtha;

public class MuhurthaLocation
{
    public string Name { get; set; } = string.Empty;

    public double Latitude { get; set; }

    public double Longitude { get; set; }

    public string Timezone { get; set; } = string.Empty;
}
