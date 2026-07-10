namespace KundaliEngine.Astrology.Astronomy
{
    public sealed class AscendantResult
    {
        public AscendantResult(double longitude)
        {
            Longitude = longitude;
        }

        public double Longitude { get; }
    }
}
