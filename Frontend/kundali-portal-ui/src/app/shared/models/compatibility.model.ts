export interface CompatibilityResponse {
  personAName: string;
  personBName: string;

  personAchart: BirthChart;
  personBchart: BirthChart;

  overallCompatibilityScore: number;
  dimensions: CompatibilityDimension[];

  overallConfidence: number;
  dominantStrengths: string[];
  dominantChallenges: string[];

  factors: string[];
  summary: string;

  analyzedAtUtc: string;
  engineVersion: string;
}

export interface BirthChart {
  birthDetails: BirthDetails;

  generatedAtUtc: string;

  ascendantSign: number;
ascendantSignAndNumber: string;
  ascendantDegree: number;
  ascendantMinute: number;

  planetPositions: PlanetPosition[];

  houses: House[];

  aspects: PlanetAspect[];

  dashaPeriods: DashaPeriod[];

  panchang: Panchang;
}

export interface BirthDetails {
  birthDateTimeLocal: string;

  latitude: number;
  longitude: number;

  timeZoneOffsetHours: number;

  placeName: string | null;
}

export interface PlanetPosition {

  planet: number;

  longitude: number;
  latitude: number;

  sign: number;

  degreeInSign: number;

  degree: number;
  minute: number;

  nakshatra: number;
  pada: number;

  house: number;

  isRetrograde: boolean;

  detailedStrength: string;

  isCombust: boolean;
}
export interface House {

  houseNumber: number;

  sign: number;

  occupants: number[];

  houseName: string;

  lifeAreas: string[];

  houseOwner: string;

  houseSwamy: string;

  hasBeneficAspect: boolean;

  hasMaleficAspect: boolean;

  score: number;

  factors: string[];

  dominantThemes: string[];
}
export interface PlanetAspect {

  fromPlanet: number;

  toPlanet: number;

  type: number;

  aspectOffset: number;

  targetHouse: number;
}
export interface Panchang {

  tithi: string;

  paksha: string;

  nakshatra: string;

  pada: number;

  yoga: string;

  karana: string;

  vaar: string;

  maas: string;

  shake: string;

  samvat: string;
}
export interface DashaPeriod {

}
export interface CompatibilityDimension {

  name: string;

  score: number;

  factors: string[];

  summary: string;

  confidence: number;
}