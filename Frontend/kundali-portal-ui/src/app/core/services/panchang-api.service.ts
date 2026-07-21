// Panchang API Service - specific endpoints for Panchang data
import { Injectable } from '@angular/core';
import { BaseApiService, ApiResponse } from './base-api.service';
import { Observable } from 'rxjs';
import { apiEndpoints } from '../../config/api-endpoints';

export interface Panchang {
  id: string;
  date: Date;
  tithi: string;
  nakshatra: string;
  yoga: string;
  karana: string;
  shake?: string;
  samvat?: string;
  dayOfWeek: string;
  sunRiseTime: string;
  sunSetTime: string;
  moonRiseTime: string;
  moonSetTime: string;
  auspiciousTimings: AuspiciousTiming[];
}

export interface AuspiciousTiming {
  type: string; // muhurat type (marriage, upanayan, etc.)
  startTime: string;
  endTime: string;
}

@Injectable({
  providedIn: 'root',
})
export class PanchangApiService extends BaseApiService {
  private endpoint = apiEndpoints.panchang;

  /**
   * Get Panchang for a specific date
   */
  getPanchang(date: Date): Observable<ApiResponse<Panchang>> {
    const formattedDate = date.toISOString().split('T')[0];
    return this.get<Panchang>(`${this.endpoint}/${formattedDate}`);
  }

  /**
   * Get Panchang for a date range
   */
  getPanchangRange(
    startDate: Date,
    endDate: Date
  ): Observable<ApiResponse<Panchang[]>> {
    const start = startDate.toISOString().split('T')[0];
    const end = endDate.toISOString().split('T')[0];
    return this.get<Panchang[]>(`${this.endpoint}/range`, {
      params: { startDate: start, endDate: end },
    });
  }

  /**
   * Get auspicious timings (muhurats) for an activity
   */
  getAuspiciousTimings(
    activityType: string,
    numberOfDates?: number
  ): Observable<ApiResponse<AuspiciousTiming[]>> {
    return this.get<AuspiciousTiming[]>(`${this.endpoint}/muhurats`, {
      params: { activityType, count: numberOfDates || 10 },
    });
  }
}
