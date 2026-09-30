using System;
using System.Collections.Generic;

namespace KundaliEngine.API.DTOs.Responses
{
    public class MahadashaAnalysisDto
    {
        public MahadashaSummaryDto Summary { get; set; } = new();
        public BirthChartSummaryDto BirthChart { get; set; } = new();
        public CurrentMahadashaDto CurrentMahadasha { get; set; } = new();
        public CurrentAntardashaDto CurrentAntardasha { get; set; } = new();
        public CurrentPratyantardashaDto CurrentPratyantardasha { get; set; } = new();
        public DashaTimelineDto Timeline { get; set; } = new();
        public List<LifeAreaDto> LifeAreas { get; set; } = new();
        public DashaStrengthDto Strength { get; set; } = new();
        public List<string> PositiveInfluences { get; set; } = new();
        public List<string> Challenges { get; set; } = new();
        public PredictionAnalysisDto Prediction { get; set; } = new();
        public RecommendedActionsDto RecommendedActions { get; set; } = new();
        public List<ImportantDateDto> ImportantDates { get; set; } = new();
        public TransitImpactDto TransitImpact { get; set; } = new();
        public RemediesDto Remedies { get; set; } = new();
        public UpcomingDashaDto UpcomingDasha { get; set; } = new();
        public List<DashaPlanetReadingDto> PlanetReadings { get; set; } = new();
        public List<DashaGocharPlanetDto> Gochar { get; set; } = new();
    }

    public class DashaGocharPlanetDto
    {
        public string Planet { get; set; } = string.Empty;
        public string Sign { get; set; } = string.Empty;
        public int HouseFromLagna { get; set; }
        public int HouseFromMoon { get; set; }
        public List<int> AspectedHouses { get; set; } = new();
    }

    public class DashaPlanetReadingDto
    {
        public string Planet { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int House { get; set; }
        public DashaLevelStrengthDto Strength { get; set; } = new();
        public List<DashaPeriodFactDto> Facts { get; set; } = new();
    }

    public class DashaPeriodFactDto
    {
        public string Planet { get; set; } = string.Empty;
        public string Tone { get; set; } = "Neutral";
        public string Text { get; set; } = string.Empty;
        public int Score { get; set; }
    }

    public class BirthChartSummaryDto
    {
        public string Name { get; set; } = string.Empty;
        public string Gender { get; set; } = string.Empty;
        public DateTime BirthDateTime { get; set; }
        public string BirthPlace { get; set; } = string.Empty;
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public string Lagna { get; set; } = string.Empty;
        public string ChandraRashi { get; set; } = string.Empty;
        public string SuryaRashi { get; set; } = string.Empty;
        public string Nakshatra { get; set; } = string.Empty;
        public int Pada { get; set; }
        public string DashaBelongsTo { get; set; } = string.Empty;
    }
    public class MahadashaSummaryDto
    {
        public int OverallScore { get; set; }
        public int OverallRating { get; set; }
        public string OverallResult { get; set; } = string.Empty;
        public string CurrentMahadasha { get; set; } = string.Empty;
        public string CurrentAntardasha { get; set; } = string.Empty;
        public string CurrentPratyantardasha { get; set; } = string.Empty;
        public string RemainingDuration { get; set; } = string.Empty;
        public string PratyantardashaRemainingDuration { get; set; } = string.Empty;
        public int CompletedPercentage { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
    }

    public class CurrentMahadashaDto
    {
        public string Planet { get; set; } = string.Empty;
        public string PlanetIcon { get; set; } = string.Empty;
        public string PlanetColor { get; set; } = string.Empty;
        public string Nature { get; set; } = string.Empty;
        public int DurationYears { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public int CurrentYear { get; set; }
        public string Description { get; set; } = string.Empty;
        public List<string> Keywords { get; set; } = new();
    }

    public class CurrentAntardashaDto
    {
        public string Planet { get; set; } = string.Empty;
        public string PlanetIcon { get; set; } = string.Empty;
        public string PlanetColor { get; set; } = string.Empty;
        public string Nature { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string Description { get; set; } = string.Empty;
        public string CombinedEffect { get; set; } = string.Empty;
    }

    public class CurrentPratyantardashaDto
    {
        public string Planet { get; set; } = string.Empty;
        public string PlanetIcon { get; set; } = string.Empty;
        public string PlanetColor { get; set; } = string.Empty;
        public string Nature { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string RemainingDuration { get; set; } = string.Empty;
    }

    public class DashaTimelineDto
    {
        public int CurrentMahadashaIndex { get; set; }
        public int CurrentAntardashaIndex { get; set; }
        public int CurrentPratyantardashaIndex { get; set; }
        public List<MahadashaTimelinePeriodDto> Mahadasha { get; set; } = new();
        public List<AntardashaTimelinePeriodDto> Antardasha { get; set; } = new();
        public List<PratyantardashaTimelinePeriodDto> Pratyantardasha { get; set; } = new();
    }

    public class MahadashaTimelinePeriodDto
    {
        public string Planet { get; set; } = string.Empty;
        public string Start { get; set; } = string.Empty;
        public string End { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public List<AntardashaTimelinePeriodDto> Antardashas { get; set; } = new();
    }

    public class AntardashaTimelinePeriodDto : MahadashaTimelinePeriodDto
    {
        public List<PratyantardashaTimelinePeriodDto> Pratyantardashas { get; set; } = new();
    }

    public class PratyantardashaTimelinePeriodDto : MahadashaTimelinePeriodDto
    {
    }

    public class LifeAreaDto
    {
        public string Name { get; set; } = string.Empty;
        public int Score { get; set; }
        public int Rating { get; set; }
        public string Status { get; set; } = string.Empty;
        public string Icon { get; set; } = string.Empty;
        public string Fact { get; set; } = string.Empty;
    }

    public class DashaStrengthDto
    {
        public int OverallStrength { get; set; }
        public string Planet { get; set; } = string.Empty;
        public string PlanetStrength { get; set; } = string.Empty;
        public string Placement { get; set; } = string.Empty;
        public string Sign { get; set; } = string.Empty;
        public bool AspectOnLagna { get; set; }
        public bool SupportedByBenefics { get; set; }
        public List<string> SupportedBy { get; set; } = new();
        public string AssociationTone { get; set; } = "None";
        public string AssociationSummary { get; set; } = "None";
        public List<string> AssociationDetails { get; set; } = new();
        public string OverallResult { get; set; } = string.Empty;
        public DashaLevelStrengthDto Mahadasha { get; set; } = new();
        public DashaLevelStrengthDto Antardasha { get; set; } = new();
        public DashaLevelStrengthDto Pratyantardasha { get; set; } = new();
    }

    public class DashaLevelStrengthDto
    {
        public string Level { get; set; } = string.Empty;
        public string Planet { get; set; } = string.Empty;
        public int OverallStrength { get; set; }
        public string PlanetStrength { get; set; } = string.Empty;
        public string Placement { get; set; } = string.Empty;
        public string Sign { get; set; } = string.Empty;
        public bool AspectOnLagna { get; set; }
        public bool SupportedByBenefics { get; set; }
        public List<string> SupportedBy { get; set; } = new();
        public string AssociationTone { get; set; } = "None";
        public string AssociationSummary { get; set; } = "None";
        public List<string> AssociationDetails { get; set; } = new();
        public string OverallResult { get; set; } = string.Empty;
    }

    public class PredictionAnalysisDto
    {
        public string SelectedCategory { get; set; } = string.Empty;
        public List<PredictionCategoryDto> Categories { get; set; } = new();
        public Dictionary<string, PredictionPhaseDto> PhaseAnalysis { get; set; } = new();
    }

    public class PredictionCategoryDto
    {
        public string Name { get; set; } = string.Empty;
        public int Score { get; set; }
        public string Prediction { get; set; } = string.Empty;
    }

    public class PredictionPhaseDto
    {
        public string Title { get; set; } = string.Empty;
        public string Period { get; set; } = string.Empty;
        public List<string> Points { get; set; } = new();
    }

    public class RecommendedActionsDto
    {
        public List<string> Dos { get; set; } = new();
        public List<string> Donts { get; set; } = new();
    }

    public class ImportantDateDto
    {
        public string Title { get; set; } = string.Empty;
        public DateTime Date { get; set; }
    }

    public class TransitImpactDto
    {
        public string Summary { get; set; } = string.Empty;
        public List<TransitImpactItemDto> MajorTransits { get; set; } = new();
    }

    public class TransitImpactItemDto
    {
        public string Planet { get; set; } = string.Empty;
        public string Impact { get; set; } = string.Empty;
    }

    public class RemediesDto
    {
        public string Mantra { get; set; } = string.Empty;
        public string Fast { get; set; } = string.Empty;
        public string Gemstone { get; set; } = string.Empty;
        public string Color { get; set; } = string.Empty;
        public string Donation { get; set; } = string.Empty;
        public string Deity { get; set; } = string.Empty;
        public string Charity { get; set; } = string.Empty;
        public string LifestyleAdvice { get; set; } = string.Empty;
    }

    public class UpcomingDashaDto
    {
        public string Planet { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public int DurationYears { get; set; }
        public string Summary { get; set; } = string.Empty;
    }
}








