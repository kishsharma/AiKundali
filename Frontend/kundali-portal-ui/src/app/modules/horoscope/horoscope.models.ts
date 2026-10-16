export type HoroscopeHorizon = 'Daily' | 'Weekly' | 'Monthly' | 0 | 1 | 2;

export interface HoroscopePeriodRequest {
  kundaliId: string;
  anchorDate: string;
  horizon?: HoroscopeHorizon;
  latitude?: number | null;
  longitude?: number | null;
  timezone?: string | null;
}

export interface HoroscopeArea {
  key: string;
  title: string;
  tone: string;
  score: number;
  prediction: string;
  facts: string[];
  do: string[];
  caution: string[];
  avoid: string[];
}

export interface HoroscopePanchanga {
  tithi: string;
  paksha: string;
  nakshatra: string;
  pada: number;
  yoga: string;
  karana: string;
  vaar: string;
  maas: string;
  taraNumber: number;
  taraName: string;
  taraTone: string;
  score: number;
  facts: string[];
}

export interface HoroscopeGocharaHit {
  planet: string;
  reference: string;
  house: number;
  sign: string;
  tone: string;
  fact: string;
}

export interface HoroscopeDashaContext {
  mahadasha: string;
  antardasha: string;
  pratyantardasha?: string | null;
  mahadashaStartUtc: string;
  mahadashaEndUtc: string;
  antardashaStartUtc: string;
  antardashaEndUtc: string;
  facts: string[];
}

export interface HoroscopeActiveFormation {
  type: string;
  key: string;
  name: string;
  tone: string;
  fact: string;
}

export interface HoroscopeHorizonExtras {
  facts: string[];
  sunriseLocal?: string | null;
  sunsetLocal?: string | null;
  peakHoraLord?: string | null;
  masa?: string | null;
  dashaChangeovers: string[];
  moonPath: string[];
}

export interface HoroscopeTithiPraveshaStatus {
  enabled: boolean;
  status: string;
  message: string;
  hierarchicalRules: string[];
}

export interface HoroscopePeriodResponse {
  horizon: HoroscopeHorizon;
  from: string;
  to: string;
  overallTone: string;
  overallScore: number;
  summaryFacts: string[];
  areas: HoroscopeArea[];
  panchanga?: HoroscopePanchanga | null;
  gocharaHits: HoroscopeGocharaHit[];
  dashaContext?: HoroscopeDashaContext | null;
  activeFormations: HoroscopeActiveFormation[];
  horizonExtras?: HoroscopeHorizonExtras | null;
  tithiPravesha: HoroscopeTithiPraveshaStatus;
  limitations: string[];
  disclaimer: string;
}

export interface HoroscopeRashifalArea {
  key: string;
  tone: string;
  line: string;
}

export interface HoroscopeRashifalItem {
  rashi: string;
  englishName: string;
  tone: string;
  score: number;
  summary: string;
  areas: HoroscopeRashifalArea[];
  gocharaHighlights: string[];
}

export interface HoroscopeRashifalResponse {
  horizon: HoroscopeHorizon;
  from: string;
  to: string;
  items: HoroscopeRashifalItem[];
  disclaimer: string;
}
