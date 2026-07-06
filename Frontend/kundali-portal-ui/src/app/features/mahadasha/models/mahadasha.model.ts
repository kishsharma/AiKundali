export interface MahadashaPeriod {
  planet: string;
  start?: string;
  end?: string;
  status?: 'completed' | 'current' | 'upcoming' | string;
}

export interface AntardashaPeriod extends MahadashaPeriod {}

export interface MahadashaSummary {
  overallScore: number;
  overallRating: number;
  overallResult: string;
  currentMahadasha: string;
  currentAntardasha: string;
  remainingDuration: string;
  completedPercentage: number;
  startDate: string;
  endDate: string;
}

export interface CurrentMahadasha {
  planet: string;
  planetIcon: string;
  planetColor: string;
  nature: string;
  durationYears: number;
  startDate: string;
  endDate: string;
  currentYear: number;
  description: string;
  keywords: string[];
}

export interface CurrentAntardasha {
  planet: string;
  planetIcon: string;
  planetColor: string;
  nature: string;
  startDate: string;
  endDate: string;
  description: string;
  combinedEffect: string;
}

export interface DashaTimeline {
  currentMahadashaIndex: number;
  currentAntardashaIndex: number;
  mahadasha: MahadashaPeriod[];
  antardasha: AntardashaPeriod[];
}

export interface LifeArea {
  name: string;
  score: number;
  rating: number;
  status: string;
  icon: string;
}

export interface DashaStrength {
  overallStrength: number;
  planet: string;
  planetStrength: string;
  placement: string;
  sign: string;
  aspectOnLagna: boolean;
  supportedByBenefics: boolean;
  supportedBy: string[];
  overallResult: string;
}

export interface PredictionCategory {
  name: string;
  score: number;
  prediction: string;
}

export interface PredictionPhase {
  title: string;
  period: string;
  points: string[];
}

export interface PredictionAnalysis {
  selectedCategory: string;
  categories: PredictionCategory[];
  phaseAnalysis: Record<'start' | 'middle' | 'end', PredictionPhase>;
}

export interface RecommendedActions {
  dos: string[];
  donts: string[];
}

export interface ImportantDate {
  title: string;
  date: string;
}

export interface TransitImpact {
  summary: string;
  majorTransits: Array<{ planet: string; impact: string }>;
}

export interface Remedies {
  mantra: string;
  fast: string;
  gemstone: string;
  color: string;
  donation: string;
  deity: string;
  charity: string;
  lifestyleAdvice: string;
}

export interface UpcomingDasha {
  planet: string;
  startDate: string;
  durationYears: number;
  summary: string;
}

export interface MahadashaAnalysis {
  summary: MahadashaSummary;
  currentMahadasha: CurrentMahadasha;
  currentAntardasha: CurrentAntardasha;
  timeline: DashaTimeline;
  lifeAreas: LifeArea[];
  strength: DashaStrength;
  positiveInfluences: string[];
  challenges: string[];
  prediction: PredictionAnalysis;
  recommendedActions: RecommendedActions;
  importantDates: ImportantDate[];
  transitImpact: TransitImpact;
  remedies: Remedies;
  upcomingDasha: UpcomingDasha;
}
