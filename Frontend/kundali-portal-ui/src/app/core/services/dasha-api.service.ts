// Dasha API Service - specific endpoints for Dasha period analysis
import { Injectable } from '@angular/core';
import { BaseApiService, ApiResponse } from './base-api.service';
import { Observable } from 'rxjs';
import { GenerateKundaliRequest, Kundali } from './kundali-api.service';
import { environment } from '../../config/environment/environment';

export interface DashaPeriod {
  planet: string;
  startDate: Date;
  endDate: Date;
  yearsLength: number;
}

export interface DashaAnalysis {
  id: string;
  kundaliId: string;
  currentDasha: DashaPeriod;
  futureDasha: DashaPeriod[];
  dashaSequence: string;
  predictions: string[];
  analysis: string;
}

@Injectable({
  providedIn: 'root',
})
export class DashaApiService extends BaseApiService {
 // private endpoint = 'dasha';
  private endpoint =`${environment.apiBaseUrl}/${environment.apiEndpoints.dasha}`;
  /**
   * Get Dasha analysis for a Kundali
   */
  getDashaAnalysis(kundaliId: string): Observable<ApiResponse<DashaAnalysis>> {
    return this.get<DashaAnalysis>(`${this.endpoint}/${kundaliId}`);
  }

  /**
   * Get current active Dasha
   */
  getCurrentDasha(kundaliId: string): Observable<ApiResponse<DashaPeriod>> {
    return this.get<DashaPeriod>(`${this.endpoint}/${kundaliId}/current`);
  }

  /**
   * Get Dasha predictions for next N years
   */
  getDashaPredictions(
    kundaliId: string,
    years: number = 10
  ): Observable<ApiResponse<DashaAnalysis>> {
    return this.get<DashaAnalysis>(
      `${this.endpoint}/${kundaliId}/predictions?years=${years}`
    );
  }

  /**
   * Get Maha Dasha period data
   */
  // getMahadashaPeriods(
  //    request: GenerateKundaliRequest
  // ): Observable<ApiResponse<DashaPeriod[]>> {
  //   return this.get<DashaPeriod[]>(`${this.endpoint}/mahadasha-periods`);
  // }

   getMahadashaPeriods(payload: any): Observable<any> {
    return this.http.post<any>(`${this.endpoint}/mahadasha-periods`, payload);
  }

}
