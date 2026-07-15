using KundaliEngine.Astrology.Astronomy;
using KundaliEngine.Astrology.Services;
using KundaliEngine.Astrology.Services.Astrology;
using KundaliEngine.Astrology.Services.Compatibility;
using KundaliEngine.Astrology.Services.Dasha;
using KundaliEngine.Astrology.Services.Panchang;
using KundaliEngine.Astrology.Services.Prediction;
using KundaliEngine.Astrology.Services.Scoring;
using KundaliEngine.KnowledgeBase.Services;

namespace KundaliEngine.Console
{
    public class AppServices
    {
        public IEphemerisProvider EphemerisProvider { get; }
        public PanchangService PanchangService { get; set; } = new();
        public HouseService HouseService { get; set; } = new();
        public PlanetStrengthService StrengthService { get; set; } = new();
        public PredictionService PredictionService { get; set; } = new();
        public AspectService AspectService { get; set; } = new();
        public ChartService ChartService { get; set; }
        public DashaService DashaService { get; set; } = new();
        public AstrologyKnowledgeService KnowledgeService { get; set; } = new();
        public NakshatraMetadataService NakshatraMetadataService { get; set; } =
            new(Path.Combine(AppContext.BaseDirectory, "Data", "Nakshatra"));
        public YoniCompatibilityService YoniCompatibilityService { get; set; } =
            new(Path.Combine(AppContext.BaseDirectory, "Data", "Compatibility", "YoniCompatibility.json"));
        public PlanetScoreService PlanetScoreService { get; set; } = new();
        public HouseScoreService HouseScoreService { get; set; } = new();
        public OverallKundaliScoreService OverallKundaliScoreService { get; set; } = new();
        public CompatibilityScoreService CompatibilityService { get; set; }

        public AppServices()
            : this(new SwissEphemerisProvider())
        {
        }

        public AppServices(IEphemerisProvider ephemerisProvider)
        {
            EphemerisProvider = ephemerisProvider;
            ChartService = new ChartService(ephemerisProvider);
            CompatibilityService = new CompatibilityScoreService(
                NakshatraMetadataService,
                KnowledgeService,
                YoniCompatibilityService);
        }
    }
}
