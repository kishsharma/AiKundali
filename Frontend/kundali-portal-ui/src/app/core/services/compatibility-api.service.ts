// Compatibility API Service - specific endpoints for compatibility analysis
import { Injectable } from '@angular/core';
import { BaseApiService, ApiResponse } from './base-api.service';
import { Observable } from 'rxjs';

export interface CompatibilityRequest {
  kundaliId1: string;
  kundaliId2: string;
}

export interface CompatibilityScore {
  id: string;
  kundaliId1: string;
  kundaliId2: string;
  overallScore: number;
  gunMilan: number;
  ganaKuta: number;
  vasya: number;
  yoni: number;
  rashi: number;
  rashiAdheepati: number;
  varna: number;
  mahendra: number;
  sthanadeepati: number;
  vedhaCheck: boolean;
  recommendations: string[];
  createdAt: Date;
  updatedAt: Date;
}

@Injectable({
  providedIn: 'root',
})
export class CompatibilityApiService extends BaseApiService {
  private endpoint = 'compatibility';

  /**
   * Analyze compatibility between two Kundali charts
   */
  analyzeCompatibility(
    request: CompatibilityRequest
  ): Observable<ApiResponse<CompatibilityScore>> {
    return this.post<CompatibilityScore>(`${this.endpoint}/analyze`, request);
  }

  /**
   * Get compatibility history
   */
  getCompatibilityHistory(): Observable<ApiResponse<CompatibilityScore[]>> {
    return this.get<CompatibilityScore[]>(`${this.endpoint}/history`);
  }

  /**
   * Get specific compatibility analysis
   */
  getCompatibilityAnalysis(id: string): Observable<ApiResponse<CompatibilityScore>> {
    return this.get<CompatibilityScore>(`${this.endpoint}/${id}`);
  }

  /**
   * Delete compatibility analysis
   */
  deleteCompatibilityAnalysis(id: string): Observable<ApiResponse<void>> {
    return this.delete<void>(`${this.endpoint}/${id}`);
  }
}
