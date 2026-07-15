// Dasha API Service - specific endpoints for Dasha period analysis
import { Injectable } from '@angular/core';
import { BaseApiService, ApiResponse } from './base-api.service';
import { Observable } from 'rxjs';
import { GenerateKundaliRequest } from './kundali-api.service';
import { environment } from '../../config/environment/environment';
import { MahadashaAnalysis } from '../../features/mahadasha/models/mahadasha.model';

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

export interface AntardashaPeriodDto {
  subPlanet: string;
  startDate: string;
  endDate: string;
}

export interface MahadashaPeriodDto {
  planet: string;
  start: string;
  end: string;
  antardashas: AntardashaPeriodDto[];
}

@Injectable({
  providedIn: 'root',
})
export class DashaApiService extends BaseApiService {
  private endpoint = `${environment.apiBaseUrl}/${environment.apiEndpoints.dasha}`;

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
   * Get compact Maha Dasha period data used by the Kundali report table.
   */
  getMahadashaPeriods(payload: GenerateKundaliRequest): Observable<MahadashaPeriodDto[]> {
    return this.http.post<MahadashaPeriodDto[]>(`${this.endpoint}/mahadasha-periods`, payload);
  }

  /**
   * Get enhanced Maha Dasha analysis used by the dedicated Dasha page.
   */
  getMahadashaAnalysis(payload: GenerateKundaliRequest): Observable<MahadashaAnalysis> {
    return this.http.post<MahadashaAnalysis>(`${this.endpoint}/mahadasha-analysis`, payload);
  }
}
