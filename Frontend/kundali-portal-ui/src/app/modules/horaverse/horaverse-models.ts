export type HoraverseTone = 'mysterious-safe';
export type HoraverseTheme = 'default-neuro-cosmic';
export type HoraverseSceneKind = 'awakening' | 'solar-ring' | 'house-walk' | 'kaala' | 'door-ahead';
export type HoraverseSupportTone = 'supportive' | 'challenging' | 'mixed' | 'neutral' | 'special';
export type HoraversePlanetNature = 'benefic' | 'malefic' | 'shadow';
export type HoraverseAspectTone = 'benefic' | 'malefic';

export interface HoraverseBirthProfile {
  name: string;
  gender: string;
  birthDate: string;
  birthTime: string;
  birthPlace: string;
  latitude: string | number | null | undefined;
  longitude: string | number | null | undefined;
}

export interface HoraverseSession {
  version: 1;
  createdAt: string;
  birthProfile: HoraverseBirthProfile;
  report: any;
  mahadashaPeriods: any[];
  tone: HoraverseTone;
  theme: HoraverseTheme;
}

export interface HoraverseScene {
  kind: HoraverseSceneKind;
  kicker: string;
  title: string;
  lines: string[];
  accent: string;
  focusPlanet?: string;
  focusHouse?: number;
}

export interface HoraversePlanetPoint {
  planet: string;
  sign: string;
  house: number | null;
  longitude: number;
  latitude: number;
  angle: number;
  radius: number;
  x: number;
  y: number;
  tone: string;
  label: string;
  degreeLabel: string;
  strength: string;
  supportTone: HoraverseSupportTone;
  nature: HoraversePlanetNature;
  hasBeneficAspect: boolean;
  hasMaleficAspect: boolean;
  aspectedBy: string[];
  isRetrograde: boolean;
  isCombust: boolean;
}

export interface HoraverseHouseArc {
  house: number;
  startAngle: number;
  endAngle: number;
  path: string;
  labelX: number;
  labelY: number;
}

export interface HoraverseHouseInsight {
  house: number;
  name: string;
  sign: string;
  lord: string;
  occupants: string[];
  aspectedBy: string[];
  hasBeneficAspect: boolean;
  hasMaleficAspect: boolean;
  score: number | null;
  themes: string[];
  lifeAreas: string[];
}

export interface HoraverseAspectRay {
  fromPlanet: string;
  toHouse: number;
  tone: HoraverseAspectTone;
  x1: number;
  y1: number;
  x2: number;
  y2: number;
}

export interface HoraverseDashaSlice {
  planet: string;
  start: string;
  end: string;
  current: boolean;
}

export interface HoraverseDashaInsight {
  mahadasha: string;
  antardasha: string;
  mahaStart: string;
  mahaEnd: string;
  antarStart: string;
  antarEnd: string;
  mahaProgress: number;
  antarProgress: number;
  antars: HoraverseDashaSlice[];
}
