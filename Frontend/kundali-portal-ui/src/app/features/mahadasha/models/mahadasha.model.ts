export interface BirthChartSummary {
  name: string;
  gender: string;
  birthDateTime: string;
  birthPlace: string;
  latitude: number;
  longitude: number;
  lagna: string;
  chandraRashi: string;
  suryaRashi: string;
  nakshatra: string;
  pada: number;
  dashaBelongsTo: string;
}
export interface MahadashaPeriod {
  planet: string;
  start?: string;
  end?: string;
  status?: 'completed' | 'current' | 'upcoming' | string;
  antardashas?: AntardashaPeriod[];
}

export interface PratyantardashaPeriod {
  planet: string;
  start?: string;
  end?: string;
  status?: 'completed' | 'current' | 'upcoming' | string;
}

export interface AntardashaPeriod extends MahadashaPeriod {
  pratyantardashas?: PratyantardashaPeriod[];
}

export interface MahadashaSummary {
  overallScore: number;
  overallRating: number;
  overallResult: string;
  currentMahadasha: string;
  currentAntardasha: string;
  currentPratyantardasha?: string;
  remainingDuration: string;
  pratyantardashaRemainingDuration?: string;
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

export interface CurrentPratyantardasha {
  planet: string;
  planetIcon: string;
  planetColor: string;
  nature: string;
  startDate: string;
  endDate: string;
  remainingDuration: string;
}

export interface DashaTimeline {
  currentMahadashaIndex: number;
  currentAntardashaIndex: number;
  currentPratyantardashaIndex: number;
  mahadasha: MahadashaPeriod[];
  antardasha: AntardashaPeriod[];
  pratyantardasha: PratyantardashaPeriod[];
}

export type LifeAreaLink = 'occupies' | 'rules' | 'aspects' | 'none';

export interface LifeAreaFactor {
  kind: 'lord' | 'mahadasha' | 'antardasha';
  weight: number;
  planet: string;
  score: number;
  base: number;
  change: string;
  dignity: string;
  house: number;
  placedHouse: number;
  link: LifeAreaLink;
  linkedHouse: number;
}

export interface LifeArea {
  name: string;
  score: number;
  rating: number;
  status: string;
  icon: string;
  fact?: string;
  formula?: string;
  factors?: LifeAreaFactor[];
}

export interface DashaLevelStrength {
  level: string;
  planet: string;
  overallStrength: number;
  planetStrength: string;
  placement: string;
  sign: string;
  aspectOnLagna?: boolean;
  supportedByBenefics?: boolean;
  supportedBy?: string[];
  associationTone?: string;
  associationSummary?: string;
  associationDetails?: string[];
  overallResult: string;
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
  associationTone?: string;
  associationSummary?: string;
  associationDetails?: string[];
  overallResult: string;
  mahadasha?: DashaLevelStrength;
  antardasha?: DashaLevelStrength;
  pratyantardasha?: DashaLevelStrength;
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

export interface DashaPeriodFact {
  planet: string;
  tone: 'Support' | 'Pressure' | 'Neutral' | string;
  text: string;
  score: number;
}

export interface DashaGocharPlanet {
  planet: string;
  sign: string;
  houseFromLagna: number;
  houseFromMoon: number;
  aspectedHouses: number[];
}

export interface DashaPlanetReading {
  planet: string;
  description: string;
  house?: number;
  strength: DashaLevelStrength;
  facts?: DashaPeriodFact[];
}

export interface MahadashaAnalysis {
  summary: MahadashaSummary;
  birthChart: BirthChartSummary;
  currentMahadasha: CurrentMahadasha;
  currentAntardasha: CurrentAntardasha;
  currentPratyantardasha: CurrentPratyantardasha;
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
  planetReadings?: DashaPlanetReading[];
  gochar?: DashaGocharPlanet[];
}












