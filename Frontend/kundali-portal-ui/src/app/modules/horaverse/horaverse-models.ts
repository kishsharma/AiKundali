export type HoraverseTone = 'mysterious-safe';
export type HoraverseTheme = 'default-neuro-cosmic';
export type HoraverseSceneKind = 'awakening' | 'solar-ring' | 'kaala' | 'inner-focus' | 'door-ahead';

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
}

export interface HoraverseHouseArc {
  house: number;
  startAngle: number;
  endAngle: number;
  path: string;
  labelX: number;
  labelY: number;
}
