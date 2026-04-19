using System;
using System.Collections.Generic;
using KundaliEngine.Domain.Enums;
using KundaliEngine.Domain.Models;

namespace KundaliEngine.Astrology.Services
{
    public class HouseService
    {
        public void AssignWholeSignHouses(double ascendantLongitude, List<PlanetPosition> planets)
        {
            if (planets is null)
                throw new ArgumentNullException(nameof(planets));

            int ascendantSignIndex = NormalizeSignIndex((int)(NormalizeLongitude(ascendantLongitude) / 30.0));

            foreach (var planet in planets)
            {
                int planetSignIndex = ((int)planet.Sign) - 1;
                int houseOffset = NormalizeSignIndex(planetSignIndex - ascendantSignIndex);
                planet.House = (HouseNumber)(houseOffset + 1);
            }
        }

        private static double NormalizeLongitude(double longitude)
        {
            longitude %= 360.0;
            if (longitude < 0)
                longitude += 360.0;

            return longitude;
        }

        private static int NormalizeSignIndex(int signIndex)
        {
            signIndex %= 12;
            if (signIndex < 0)
                signIndex += 12;

            return signIndex;
        }
    }
}
