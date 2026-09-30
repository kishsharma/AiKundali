// Shared API DTOs (Data Transfer Objects)

// ===========================
// Common Response Models
// ===========================

export interface ApiErrorResponse {
  timestamp: Date;
  status: number;
  error: string;
  message: string;
  path: string;
}

export interface PagedResponse<T> {
  items: T[];
  totalItems: number;
  pageNumber: number;
  pageSize: number;
  totalPages: number;
  hasNextPage: boolean;
  hasPreviousPage: boolean;
}

// ===========================
// User Models
// ===========================

export interface User {
  id: string;
  email: string;
  firstName: string;
  lastName: string;
  dateOfBirth?: Date;
  gender?: 'male' | 'female' | 'other';
  createdAt: Date;
  updatedAt: Date;
}

export interface CreateUserRequest {
  email: string;
  firstName: string;
  lastName: string;
  password: string;
}

// ===========================
// Location Models
// ===========================

export interface Location {
  id: string;
  name: string;
  latitude: number;
  longitude: number;
  timezone: string;
  country: string;
  state?: string;
}

// ===========================
// Validation Models
// ===========================

export interface ValidationError {
  field: string;
  message: string;
}

export interface FormValidationResult {
  isValid: boolean;
  errors: ValidationError[];
}

export type PredictionLifeArea = 'Relationships' | 'Career' | 'Finance' | 'Education' | 'Health' | 'Family' | 'Property';
export type PredictionFocusKey =
  | 'marriage_possibility'
  | 'marriage_timing'
  | 'career_possibility'
  | 'career_timing'
  | 'income_possibility'
  | 'income_timing'
  | 'savings_possibility'
  | 'savings_timing'
  | 'property_possibility'
  | 'property_timing'
  | 'education_possibility'
  | 'education_timing'
  | 'children_possibility'
  | 'children_timing'
  | 'health_possibility'
  | 'health_timing'
  | 'family_possibility'
  | 'family_timing';
export type PredictionTimeScope = 'Current' | 'Future';

export interface PredictionQuestionRequest {
  lifeArea: PredictionLifeArea;
  focusKey: PredictionFocusKey;
  timeScope: PredictionTimeScope;
  referenceDate?: string;
  birthDate?: string;
  kundaliData: any;
}

export interface PredictionResponse {
  lifeArea: string;
  timeScope: string;
  referenceDate: string;
  focusKey?: string | null;
  claims: PredictionClaimResponse[];
  synthesis?: PredictionSynthesisResponse | null;
  answer?: PredictionAnswerResponse | null;
}

export interface PredictionClaimResponse {
  key: string;
  tone: string;
  stage: string;
  dashaLevel?: string | null;
  dashaPlanet?: string | null;
  startDate?: string | null;
  endDate?: string | null;
  ruleIds: string[];
  evidenceKeys: string[];
}

export interface PredictionSynthesisResponse {
  overallTone?: string | null;
  effectiveFrom?: string | null;
  effectiveTo?: string | null;
  houseConnections: PredictionHouseConnectionResponse[];
}

export interface PredictionHouseConnectionResponse {
  sourceHouse: number;
  lordPlanet: string;
  targetHouse: number;
  sourceSignificationKeys: string[];
  targetSignificationKeys: string[];
  evidenceKey: string;
}

export interface PredictionAnswerResponse {
  outcome: string;
  reasonKeys: string[];
  evidenceKeys: string[];
  timingWindows: PredictionTimingWindowResponse[];
}

export interface PredictionTimingWindowResponse {
  startDate: string;
  endDate: string;
  strength: string;
  triggerPlanets: string[];
  evidenceKeys: string[];
}
