import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { catchError, timeout } from 'rxjs/operators';
import { BaseApiService } from './base-api.service';
import { apiEndpoints } from '../../config/api-endpoints';

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
  private endpoint = apiEndpoints.muhurtha;

  find(request: FindMuhurthaRequest): Observable<MuhurthaResponse> {
    return this.http
      .post<MuhurthaResponse>(`${this.apiUrl}/${this.endpoint}/find`, request)
      .pipe(
        timeout(this.apiTimeout),
        catchError((error) => this.handleError(error))
      );
  }

  searchGrahaPravesha(request: GrahaPraveshaSearchRequest): Observable<GrahaPraveshaSearchResponse> {
    return this.http
      .post<GrahaPraveshaSearchResponse>(`${this.apiUrl}/muhurtha/graha-pravesha/search`, request)
      .pipe(
        timeout(this.apiTimeout),
        catchError((error) => this.handleError(error))
      );
  }

  analyzeGrahaPraveshaDate(date: string, location: MuhurthaLocation, calendarSystem: string): Observable<GrahaPraveshaDayResponse> {
    const params = new URLSearchParams({
      latitude: String(location.latitude),
      longitude: String(location.longitude),
      timezone: location.timezone,
      city: location.name,
      calendarSystem,
    });
    return this.http
      .get<GrahaPraveshaDayResponse>(`${this.apiUrl}/muhurtha/graha-pravesha/${date}?${params.toString()}`)
      .pipe(
        timeout(this.apiTimeout),
        catchError((error) => this.handleError(error))
      );
  }
}

export interface GrahaPraveshaSearchRequest {
  location: { city: string; latitude: number; longitude: number; timezone: string };
  fromDate: string;
  toDate: string;
  calendarSystem: string;
  birthChart?: null;
}

export interface GrahaPraveshaEvidence {
  ruleId: string;
  category: string;
  input: string;
  result: string;
  severity: string;
  message: string;
  source: string;
  scoreImpact: number;
}

export interface GrahaPraveshaPeriod {
  ruleKey: string;
  name: string;
  start: string;
  end: string;
}

export interface GrahaPraveshaWindow {
  date: string;
  start: string;
  end: string;
  startTime: string;
  endTime: string;
  status: string;
  score: number;
  lagna: string;
  tithi: string;
  paksha: string;
  nakshatra: string;
  yoga: string;
  karana: string;
  vara: string;
  moonSign: string;
  kalashaCount: number;
  kalashaEligible: boolean;
  vastuTableMatched: boolean;
  evidence: GrahaPraveshaEvidence[];
  timingRestrictions: GrahaPraveshaPeriod[];
}

export interface GrahaPraveshaSearchResponse {
  eventType: string;
  profileId: string;
  profileVersion: string;
  calendarSystem: string;
  location: { city: string; latitude: number; longitude: number; timezone: string };
  searchRange: { from: string; to: string };
  results: GrahaPraveshaWindow[];
  rejectionCounts: Record<string, number>;
  knownGaps: string[];
}

export interface GrahaPraveshaDayResponse {
  date: string;
  sunrise: string;
  sunset: string;
  intervals: GrahaPraveshaWindow[];
  timingRestrictions: GrahaPraveshaPeriod[];
  knownGaps: string[];
}




