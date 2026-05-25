
namespace KundaliEngine.Astrology.Services.House
{
    public static class HouseMetadataService
    {
        public static string GetHouseName(int house)
        {
            return house switch
            {
                1 => "Self & Personality",
                2 => "Wealth & Family",
                3 => "Courage & Communication",
                4 => "Home & Mother",
                5 => "Intelligence & Children",
                6 => "Health & Enemies",
                7 => "Marriage & Partnership",
                8 => "Transformation & Longevity",
                9 => "Fortune & Dharma",
                10 => "Career & Profession",
                11 => "Gains & Networking",
                12 => "Losses & Spirituality",
                _ => "Unknown"
            };
        }

        public static List<string> GetLifeAreas(int house)
        {
            return house switch
            {
                1 => new() { "Self", "Personality", "Body", "Confidence" },

                2 => new() { "Wealth", "Family", "Speech", "Savings" },

                3 => new() { "Communication", "Courage", "Skills", "Siblings" },

                4 => new() { "Home", "Mother", "Vehicles", "Emotional Peace" },

                5 => new() { "Education", "Children", "Creativity", "Intelligence" },

                6 => new() { "Health", "Debt", "Competition", "Enemies" },

                7 => new() { "Marriage", "Relationships", "Partnerships" },

                8 => new() { "Transformation", "Secrets", "Longevity", "Research" },

                9 => new() { "Luck", "Dharma", "Spirituality", "Guru" },

                10 => new() { "Career", "Profession", "Leadership", "Status" },

                11 => new() { "Income", "Networking", "Gains", "Friends" },

                12 => new() { "Spirituality", "Foreign", "Losses", "Meditation" },

                _ => new() { "Unknown" }
            };
        }
    }
}
