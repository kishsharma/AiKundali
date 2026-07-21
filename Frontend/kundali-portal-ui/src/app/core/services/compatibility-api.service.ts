// Compatibility API Service - specific endpoints for compatibility analysis
import { Injectable } from '@angular/core';
import { BaseApiService, ApiResponse } from './base-api.service';
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

@Injectable({
  providedIn: 'root',
})
export class CompatibilityApiService extends BaseApiService {
  private endpoint = apiEndpoints.compatibility;

  /**
   * Analyze compatibility between two full birth profiles.
   */
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

  /**
   * Get compatibility history
   */
  getCompatibilityHistory(): Observable<ApiResponse<CompatibilityResponse[]>> {
    return this.get<CompatibilityResponse[]>(`${this.endpoint}/history`);
  }

  /**
   * Get specific compatibility analysis
   */
  getCompatibilityAnalysis(id: string): Observable<ApiResponse<CompatibilityResponse>> {
    return this.get<CompatibilityResponse>(`${this.endpoint}/${id}`);
  }

  
  /**
   * Delete compatibility analysis
   */
  deleteCompatibilityAnalysis(id: string): Observable<ApiResponse<void>> {
    return this.delete<void>(`${this.endpoint}/${id}`);
  }
}
