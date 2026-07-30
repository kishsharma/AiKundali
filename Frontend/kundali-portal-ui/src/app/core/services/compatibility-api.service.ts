// Compatibility API Service - specific endpoints for compatibility analysis
import { Injectable } from '@angular/core';
import { BaseApiService } from './base-api.service';
import { Observable } from 'rxjs';
import { catchError, timeout } from 'rxjs/operators';
import { CompatibilityResponse } from '../../shared/models/compatibility.model';
import { apiEndpoints } from '../../config/api-endpoints';

export interface MatchRequestPerson {
  name: string;
  gender: string;
  birthDateTime: string;
  birthPlace: string;
  latitude: number;
  longitude: number;
  timeZoneOffset: number;
}

export interface CompatibilityRequest {
  personA: MatchRequestPerson;
  personB: MatchRequestPerson;
}

export interface CompatibilityDimension {
  name: string;
  score: number;
  factors: string[];
  summary: string;
  confidence: number;
}

export interface SaveCompatibilityRequest {
  request: CompatibilityRequest;
  result: CompatibilityResponse;
}

export interface SavedCompatibilityListItem {
  id: string;
  displayName: string;
  personAName: string;
  personADateOfBirth: string;
  personABirthPlace: string;
  personBName: string;
  personBDateOfBirth: string;
  personBBirthPlace: string;
  overallCompatibilityScore: number;
  ashtakootaScore?: number | null;
  createdAt: string;
}

export interface SavedCompatibilityDetail extends SavedCompatibilityListItem {
  personAGender?: string | null;
  personATimeOfBirth: string;
  personALatitude?: number | null;
  personALongitude?: number | null;
  personBGender?: string | null;
  personBTimeOfBirth: string;
  personBLatitude?: number | null;
  personBLongitude?: number | null;
  request: CompatibilityRequest;
  result: CompatibilityResponse;
}

export interface SendCompatibilityReportEmailRequest {
  toEmail: string;
  subject: string;
  message: string;
  fileName: string;
  mode: string;
  attachmentFileName: string;
  attachmentContentType: string;
  attachmentBase64: string;
  birthProfile: Record<string, unknown>;
  report: CompatibilityResponse;
}

@Injectable({
  providedIn: 'root',
})
export class CompatibilityApiService extends BaseApiService {
  private endpoint = apiEndpoints.compatibility;

  match(request: CompatibilityRequest): Observable<CompatibilityResponse> {
    return this.http
      .post<CompatibilityResponse>(`${this.apiUrl}/${this.endpoint}/match`, request)
      .pipe(
        timeout(this.apiTimeout),
        catchError((error) => this.handleError(error))
      );
  }

  analyzeCompatibility(request: CompatibilityRequest): Observable<CompatibilityResponse> {
    return this.match(request);
  }

  saveCompatibility(request: SaveCompatibilityRequest): Observable<SavedCompatibilityDetail> {
    return this.http
      .post<SavedCompatibilityDetail>(`${this.apiUrl}/${this.endpoint}/save`, request)
      .pipe(
        timeout(this.apiTimeout),
        catchError((error) => this.handleError(error))
      );
  }

  getCompatibilityHistory(): Observable<SavedCompatibilityListItem[]> {
    return this.http
      .get<SavedCompatibilityListItem[]>(`${this.apiUrl}/${this.endpoint}/history`)
      .pipe(
        timeout(this.apiTimeout),
        catchError((error) => this.handleError(error))
      );
  }

  getCompatibilityAnalysis(id: string): Observable<SavedCompatibilityDetail> {
    return this.http
      .get<SavedCompatibilityDetail>(`${this.apiUrl}/${this.endpoint}/${id}`)
      .pipe(
        timeout(this.apiTimeout),
        catchError((error) => this.handleError(error))
      );
  }

  sendReportEmail(request: SendCompatibilityReportEmailRequest): Observable<any> {
    return this.http
      .post<any>(`${this.apiUrl}/${apiEndpoints.reportShare.sendEmail}`, request)
      .pipe(
        timeout(180000),
        catchError((error) => this.handleError(error))
      );
  }

  deleteCompatibilityAnalysis(id: string): Observable<void> {
    return this.http
      .delete<void>(`${this.apiUrl}/${this.endpoint}/${id}`)
      .pipe(
        timeout(this.apiTimeout),
        catchError((error) => this.handleError(error))
      );
  }
}