import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { catchError, timeout } from 'rxjs/operators';
import { BaseApiService } from './base-api.service';

export interface MuhurthaLocation {
  name: string;
  latitude: number;
  longitude: number;
  timezone: string;
}

export interface MuhurthaHoroscopeReference {
  name?: string | null;
  birthDateTime?: string | null;
  birthPlace?: string | null;
  latitude?: number | null;
  longitude?: number | null;
  timezone?: string | null;
}

export interface MuhurthaUserFilters {
  avoidRahuKalam: boolean;
  avoidBhadra: boolean;
  preferGoodChoghadiya: boolean;
  checkPersonalHoroscope: boolean;
  checkMuhurthaLagna: boolean;
}

export interface FindMuhurthaRequest {
  eventType: number;
  regionalProfile: number;
  location: MuhurthaLocation;
  requestedStart: string;
  requestedEnd: string;
  dailyStartTime?: string | null;
  dailyEndTime?: string | null;
  horoscope?: MuhurthaHoroscopeReference | null;
  filters: MuhurthaUserFilters;
}

export interface MuhurthaTimingDetail {
  name: string;
  startTime?: string | null;
  endTime?: string | null;
}

export interface MuhurthaPanchangTimings {
  tithi: MuhurthaTimingDetail;
  nakshatra: MuhurthaTimingDetail;
  karana: MuhurthaTimingDetail;
  yoga: MuhurthaTimingDetail;
  chandraRashi: MuhurthaTimingDetail;
  lagna: MuhurthaTimingDetail;
}

export interface MuhurthaScoreBreakdown {
  totalScore: number;
  genericTimeRulesScore: number;
  panchangaRulesScore: number;
  personalHoroscopeRulesScore: number;
  muhurthaChartRulesScore: number;
}


export interface MuhurthaRuleResult {
  ruleKey: string;
  status: number | string;
  severity: number | string;
  scoreImpact: number;
  hardRejected: boolean;
  startTime?: string | null;
  endTime?: string | null;
  summary?: string | null;
  recommendation?: string | null;
  metadata?: Record<string, string>;
}
export interface MuhurthaRecommendation {
  slotId: string;
  startTime: string;
  endTime: string;
  score: number;
  verdict: number | string;
  hardRejected: boolean;
  strengths: string[];
  warnings: string[];
  ruleResults: MuhurthaRuleResult[];
  panchangTimings: MuhurthaPanchangTimings;
  scoreBreakdown: MuhurthaScoreBreakdown;
}

export interface MuhurthaResponse {
  eventType: number;
  regionalProfile: number;
  location: MuhurthaLocation;
  requestedStart: string;
  requestedEnd: string;
  usedPersonalHoroscope: boolean;
  unsupportedCalculations: string[];
  recommendedSlots: MuhurthaRecommendation[];
}

@Injectable({ providedIn: 'root' })
export class MuhurthaApiService extends BaseApiService {
  private endpoint = 'Muhurtha';

  find(request: FindMuhurthaRequest): Observable<MuhurthaResponse> {
    return this.http
      .post<MuhurthaResponse>(`${this.apiUrl}/${this.endpoint}/find`, request)
      .pipe(
        timeout(this.apiTimeout),
        catchError((error) => this.handleError(error))
      );
  }
}




