import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';

import { BaseApiService } from './base-api.service';

export interface DashboardUser {
  id: string;
  fullName: string;
  email: string;
  role: string;
}

export interface DashboardDaySlot {
  name: string;
  start: string;
  end: string;
}

export interface DashboardPanchang {
  date: string;
  tithi: string;
  paksha: string;
  nakshatra: string;
  pada: number;
  yoga: string;
  karana: string;
  vaar: string;
  maas: string;
  sunRiseTime?: string | null;
  sunSetTime?: string | null;
  goodChoghadiya?: DashboardDaySlot[] | null;
  rahuKal?: DashboardDaySlot | null;
  locationName: string;
}

export interface DashboardCollectionStats {
  planCode?: string;
  savedKundalis: number;
  kundaliSaveLimit?: number;
  kundaliSavesLeft?: number;
  savedMatches: number;
  matchSaveLimit?: number;
  matchSavesLeft?: number;
  pdfGenerated?: number;
  pdfUsedToday?: number;
  pdfDailyLimit?: number | null;
  pdfLeftToday?: number | null;
  emailsSent?: number;
  emailIncluded?: boolean;
}

export interface DashboardKundali {
  id: string;
  displayName: string;
  fullName: string;
  dateOfBirth: string;
  timeOfBirth: string;
  birthPlace: string;
  createdAt: string;
}

export interface DashboardCurrentDasha {
  mahadasha: string;
  antardasha: string;
  startDate: string;
  endDate: string;
  antardashaStartDate: string;
  antardashaEndDate: string;
  summary: string;
}

export interface DashboardLearningLevel {
  id: string;
  code: string;
  slug: string;
  title: string;
  subtitle?: string;
  description: string;
  sessionCount: number;
  estimatedMinutes: number;
  estimatedHours: number;
  status: string;
}

export interface DashboardRecentReport {
  id: string;
  kundaliId: string;
  reportType: string;
  reportStatus: string;
  kundaliName: string;
  createdAt: string;
}

export interface DashboardMuhurthaPreview {
  date: string;
  title: string;
  timeWindow: string;
  quality: string;
}

export interface DashboardInsight {
  eyebrow: string;
  title: string;
  text: string;
  routerLink: string;
  action: string;
  tone: 'moon' | 'study' | 'muhurtha' | string;
}

export interface RetrogradePlanet {
  planet: string;
  currentSign: string;
  retrogradeFromSign: string;
  retrogradeTowardSign: string;
  degree: number;
  minute: number;
  degreeInSign: number;
  longitude: number;
  isNode: boolean;
  motion: string;
  information: string;
}

export interface CurrentRetrogradesResponse {
  calculatedAtUtc: string;
  includeNodes: boolean;
  note: string;
  planets: RetrogradePlanet[];
}

export interface DashboardSummary {
  user: DashboardUser;
  todayPanchang: DashboardPanchang;
  collectionStats: DashboardCollectionStats;
  latestKundali?: DashboardKundali | null;
  currentDasha?: DashboardCurrentDasha | null;
  learningSummary?: DashboardLearningLevel | null;
  learningLevels: DashboardLearningLevel[];
  recentReport?: DashboardRecentReport | null;
  upcomingMuhurthas: DashboardMuhurthaPreview[];
  dailyInsights: DashboardInsight[];
}

@Injectable({
  providedIn: 'root',
})
export class DashboardApiService extends BaseApiService {
  getSummary(): Observable<DashboardSummary> {
    return this.http.get<DashboardSummary>(`${this.apiUrl}/Dashboard/summary`);
  }

  getCurrentRetrogrades(includeNodes = false): Observable<CurrentRetrogradesResponse> {
    return this.http.get<CurrentRetrogradesResponse>(`${this.apiUrl}/gochar/retrogrades/current`, {
      params: { includeNodes }
    });
  }
}
