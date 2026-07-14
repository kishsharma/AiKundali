namespace KundaliEngine.Domain.Models.Muhurtha;

public class MuhurthaUserFilters
{
    public bool AvoidRahuKalam { get; set; } = true;

    public bool AvoidBhadra { get; set; } = true;

    public bool PreferGoodChoghadiya { get; set; } = false;

    public bool CheckPersonalHoroscope { get; set; } = false;

    public bool CheckMuhurthaLagna { get; set; } = false;
}
