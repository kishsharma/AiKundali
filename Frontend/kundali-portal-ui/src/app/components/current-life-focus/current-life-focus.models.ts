export interface CurrentLifeFocusRequest {
  kundaliId: string;
  analysisDate: string;
}

export interface CurrentLifeFocusArea {
  key: string;
  title: string;
  activationLevel: string;
  experienceTone?: string;
  tone?: string;
  score?: number;
  activationScore?: number;
  confidence?: number;
  activatedHouses: number[];
  primaryFactors?: string[];
  shortReasons?: string[];
}

export interface CurrentLifeFocusResponse {
  analysisId: string;
  calculatedAtUtc?: string;
  analysisDate?: string;
  currentMahadasha?: string;
  currentAntardasha?: string;
  activeDasha?: {
    mahadasha?: string;
    antardasha?: string;
  };
  areas: CurrentLifeFocusArea[];
  prompt?: string;
  question?: string;
}

export interface FocusedLifeAreaRequest {
  selectedAreaKey: string;
  specificConcernKey?: string;
}

export interface SpecificConcernOption {
  key: string;
  title?: string;
  label?: string;
}

export interface FocusedActivatedHouse {
  house: number;
  role?: string;
  activationScore?: number;
}

export interface FocusedLifeAreaNarrative {
  verdict: string;
  currentSituation: string;
  astrologicalReason: string;
  futureChange: string;
  importantPeriods: string;
  expectedOutcome: string;
  evidence: string[];
}

export interface FocusedDecisionGuidance {
  questionType: string;
  verdictLabel: string;
  recommendation: string;
  why: string;
  bestApproach: string;
  suitableFor: string[];
  cautions: string[];
}
export interface FocusedLifeAreaResponse {
  areaKey: string;
  title: string;
  activationLevel: string;
  experienceTone?: string;
  tone?: string;
  summary?: string;
  narrative?: FocusedLifeAreaNarrative;
  decisionGuidance?: FocusedDecisionGuidance;
  calculationBasis?: string[];
  possibleExperiences: string[];
  supportiveFactors: string[];
  pressureFactors: string[];
  activatedHouses: Array<number | FocusedActivatedHouse>;
  followUpQuestion?: string;
  specificConcerns?: SpecificConcernOption[];
  followUpOptions?: SpecificConcernOption[];
}

export type CurrentLifeFocusState =
  | 'initial'
  | 'loadingTop'
  | 'topLoaded'
  | 'loadingFocus'
  | 'focusLoaded'
  | 'noStrongActivation'
  | 'noneSelected'
  | 'otherConcern'
  | 'error';

