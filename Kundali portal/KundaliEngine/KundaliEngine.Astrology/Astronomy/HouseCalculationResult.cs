using System.Collections.Generic;

namespace KundaliEngine.Astrology.Astronomy
{
    public sealed class HouseCalculationResult
    {
        public HouseCalculationResult(IReadOnlyList<double> cusps, IReadOnlyList<double> ascMc)
        {
            Cusps = cusps;
            AscMc = ascMc;
        }

        public IReadOnlyList<double> Cusps { get; }
        public IReadOnlyList<double> AscMc { get; }
    }
}
