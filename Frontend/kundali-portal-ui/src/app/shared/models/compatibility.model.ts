export interface CompatibilityResponse {
  personAName: string;
  personBName: string;

  personAchart: BirthChart;
  personBchart: BirthChart;
  ashtakoota: AshtakootaResult;
  doshaAnalysis: CompatibilityDoshaAnalysis;
  relationshipHouseAnalysis: CompatibilityRelationshipHouseAnalysis[];

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

  ascendantSign: number | string;
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

  planet: number | string;

  longitude: number;
  latitude: number;

  sign: number | string;

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

  sign: number | string;

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

  akshara: string;

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

  scoreBreakdown: CompatibilityScoreComponent[];

  summary: string;

  confidence: number;
}

export interface CompatibilityScoreComponent {
  label: string;
  score: number;
  weight: number;
  contribution: number;
}

export interface CompatibilityDoshaAnalysis {
  overallDoshaScore: number;
  overallDoshaStatus: string;
  doshaFactors: string[];
  doshaRemedies: string[];
  items: CompatibilityDoshaItem[];
  scoreBreakdown: CompatibilityScoreComponent[];
}

export interface CompatibilityDoshaItem {
  key: string;
  name: string;
  category: string;
  status: string;
  compatibilityScore: number;
  riskScore: number;
  severity: string;
  tone: string;
  personA: CompatibilityDoshaPersonSummary;
  personB: CompatibilityDoshaPersonSummary;
  reason: string;
  factors: string[];
  remedies: string[];
  references: string[];
  parasharaReference?: CompatibilityParasharaReference | null;
  scoreBreakdown: CompatibilityScoreComponent[];
}

export interface CompatibilityParasharaReference {
  sourceText: string;
  chapter: number;
  chapterTitle: string;
  verses: string;
  sanskrit: string;
  transliteration: string;
  simpleMeaning: string;
  referenceType: string;
  verificationStatus: string;
  scopeNote: string;
  editionNote: string;
}

export interface CompatibilityDoshaPersonSummary {
  isPresent: boolean;
  severity: string;
  reason: string;
  involvedPlanets: string[];
  involvedHouses: number[];
  mitigatingFactors: string[];
}

export interface CompatibilityRelationshipHouseAnalysis {
  houseNumber: number;
  themeKey: string;
  personA: CompatibilityHouseEvidence;
  personB: CompatibilityHouseEvidence;
  combinedScore: number | null;
  concernLevel: 'Low' | 'Moderate' | 'High' | 'Unavailable';
}

export interface CompatibilityHouseEvidence {
  isAvailable: boolean;
  supportScore: number | null;
  concernLevel: 'Low' | 'Moderate' | 'High' | 'Unavailable';
  houseLord: string;
  houseLordStrength: number | null;
  occupants: string[];
  beneficInfluences: string[];
  maleficInfluences: string[];
  mitigatingFactors: string[];
  factors: string[];
}

export interface AshtakootaResult {
  totalScore: number;
  maxScore: number;
  scores: KutaScore[];
}

export interface KutaScore {
  name: string;
  score: number;
  maxScore: number;
  grade: string;
  detail: string;
}
