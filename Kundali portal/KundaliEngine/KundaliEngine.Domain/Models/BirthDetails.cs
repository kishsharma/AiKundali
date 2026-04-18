using System;

namespace KundaliEngine.Domain.Models
{
    public class BirthDetails
    {
        // Local civil birth date/time at the place of birth.
        public DateTime BirthDateTimeLocal { get; set; }

        // Geographic latitude in degrees (positive north).
        public double Latitude { get; set; }

        // Geographic longitude in degrees (positive east).
        public double Longitude { get; set; }

        // Time zone offset from UTC in hours at the time of birth (e.g. +5.5 for IST).
        public double TimeZoneOffsetHours { get; set; }

        public string? PlaceName { get; set; }
    }
}
