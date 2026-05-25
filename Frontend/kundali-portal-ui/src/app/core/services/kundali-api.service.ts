// Kundali API Service - specific endpoints for Kundali operations
import { Injectable } from '@angular/core';
import { BaseApiService, ApiResponse } from './base-api.service';
import { Observable } from 'rxjs';
import { environment } from '../../config/environment/environment';

export interface Kundali {
  id: string;
  userId: string;
  name: string;
  birthDateTime: Date;
  latitude: number;
  longitude: number;
  timeZoneOffset: number;
  createdAt: Date;
  updatedAt: Date;
}

export interface GenerateKundaliRequest {
  name: string;
  birthDateTime: string; // ISO 8601 format: 2026-05-20T15:10:00.290Z
  latitude: number;
  longitude: number;
  timeZoneOffset: number;
}

@Injectable({
  providedIn: 'root',
})
export class KundaliApiService extends BaseApiService {
  private kundaliEndpoints = environment.apiEndpoints.kundali;

  /**
   * Get all Kundali charts for the current user
   */
  getKundaliCharts(): Observable<ApiResponse<Kundali[]>> {
    return this.get<Kundali[]>(this.kundaliEndpoints.getAll);
  }

  /**
   * Get a specific Kundali chart
   */
  getKundaliChart(id: string): Observable<ApiResponse<Kundali>> {
    const endpoint = this.kundaliEndpoints.getById.replace(':id', id);
    return this.get<Kundali>(endpoint);
  }

  /**
   * Generate a new Kundali chart
   */
  generateKundali(
    request: GenerateKundaliRequest
  ): Observable<ApiResponse<Kundali>> {
    return this.post<Kundali>(this.kundaliEndpoints.generate, request);
  }

  /**
   * Update existing Kundali chart
   */
  updateKundali(id: string, request: Partial<GenerateKundaliRequest>): Observable<ApiResponse<Kundali>> {
    const endpoint = this.kundaliEndpoints.update.replace(':id', id);
    return this.put<Kundali>(endpoint, request);
  }

  /**
   * Delete Kundali chart
   */
  deleteKundali(id: string): Observable<ApiResponse<void>> {
    const endpoint = this.kundaliEndpoints.delete.replace(':id', id);
    return this.delete<void>(endpoint);
  }
}
