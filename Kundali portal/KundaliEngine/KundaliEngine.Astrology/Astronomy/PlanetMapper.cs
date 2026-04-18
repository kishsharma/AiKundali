internal static class PlanetMapper
{
    public static int GetPlanetId(string name)
    {
        return name.ToLower() switch
        {
            "sun" => 0,
            "moon" => 1,
            "mercury" => 2,
            "venus" => 3,
            "mars" => 4,
            "jupiter" => 5,
            "saturn" => 6,
            "rahu" => 11,
            _ => throw new ArgumentException("Invalid planet")
        };
    }
}
