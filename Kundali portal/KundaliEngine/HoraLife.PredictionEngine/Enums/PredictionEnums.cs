using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HoraLife.PredictionEngine.Enums
{
    public enum PredictionLifeArea
    {
        Career,
        Finance,
        Relationships,
        Education,
        Health,
        Family,
        Property
    }

    public enum PredictionMode
    {
        Report,
        CustomQuestion
    }
    public enum PredictionTimeScope
    {
        Current,
        Past,
        Future,
        DateRange
    }
    public enum DashaLevel
    {
        Mahadasha,
        Antardasha,
        Pratyantardasha
    }

    public enum PredictionTone
    {
        Supportive,
        Challenging,
        Mixed,
        Neutral
    }
    public enum PredictionClaimStage
    {
        Natal,
        Dasha,
        Consolidated
    }
}
