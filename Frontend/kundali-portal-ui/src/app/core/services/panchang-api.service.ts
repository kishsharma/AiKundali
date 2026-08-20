// Panchang API Service - specific endpoints for Panchang data
import { Injectable } from '@angular/core';
import { BaseApiService, ApiResponse } from './base-api.service';
import { Observable } from 'rxjs';
import { apiEndpoints } from '../../config/api-endpoints';

export type PanchangCalendarMode = 'daily' | 'weekly' | 'monthly';

export interface NakshatraPadaTiming {
  pada: number;
  syllable: string;
  startTime: string;
  endTime: string;
  isCurrent: boolean;
}

export interface Panchang {
  id: string;
  date: Date | string;
  tithi: string;
  tithiEndTime?: string;
  paksha?: string;
  nakshatra: string;
  nakshatraEndTime?: string;
  pada?: number;
  akshara?: string;
  nakshatraPadas?: NakshatraPadaTiming[];
  yoga: string;
  yogaEndTime?: string;
  karana: string;
  karanaEndTime?: string;
  vaar?: string;
  maas?: string;
  shake?: string;
  samvat?: string;
  dayOfWeek: string;
  sunRiseTime: string;
  sunSetTime: string;
  moonRiseTime: string;
  moonSetTime: string;
  isAuspicious?: boolean;
  summary?: string;
  auspiciousTimings: AuspiciousTiming[];
}

export interface LagnaTiming {
  sign: string;
  lord: string;
  endTime: string;
}

export interface PanchangLagnaTimingsResponse {
  date: string;
  sunrise: string;
  nextSunrise: string;
  timings: LagnaTiming[];
}
export interface PanchangCalendarResponse {
  mode: PanchangCalendarMode;
  startDate: string;
  endDate: string;
  latitude: number;
  longitude: number;
  timezone: string;
  days: Panchang[];
}

export interface PanchangEclipse {
  id: string;
  kind: 'solar' | 'lunar' | string;
  type: string;
  date: string;
  startTime: string;
  maximumTime: string;
  endTime: string;
  sutakStartTime?: string | null;
  sutakEndTime?: string | null;
  isVisibleFromLocation: boolean;
}

export interface PanchangEclipseYearResponse {
  year: number;
  latitude: number;
  longitude: number;
  timezone: string;
  eclipses: PanchangEclipse[];
}

export interface PanchangCalendarRequest {
  mode: PanchangCalendarMode;
  date: Date;
  latitude?: number;
  longitude?: number;
  timezone?: string;
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
   * Get Panchang calendar for daily, weekly, or monthly mode.
   */
  getPanchangCalendar(request: PanchangCalendarRequest): Observable<ApiResponse<PanchangCalendarResponse>> {
    return this.get<PanchangCalendarResponse>(`${this.endpoint}/calendar`, {
      params: {
        mode: request.mode,
        date: this.formatDate(request.date),
        latitude: request.latitude ?? 12.9716,
        longitude: request.longitude ?? 77.5946,
        timezone: request.timezone || 'Asia/Kolkata',
      },
    });
  }

  getLagnaTimings(
    date: Date,
    latitude?: number,
    longitude?: number,
    timezone?: string
  ): Observable<ApiResponse<PanchangLagnaTimingsResponse>> {
    return this.get<PanchangLagnaTimingsResponse>(`${this.endpoint}/lagna-timings/${this.formatDate(date)}`, {
      params: {
        latitude: latitude ?? 12.9716,
        longitude: longitude ?? 77.5946,
        timezone: timezone || 'Asia/Kolkata',
      },
    });
  }
  /**
   * Get yearly visible eclipses for the selected location.
   */
  getYearlyEclipses(
    year: number,
    latitude?: number,
    longitude?: number,
    timezone?: string
  ): Observable<ApiResponse<PanchangEclipseYearResponse>> {
    return this.get<PanchangEclipseYearResponse>(`${this.endpoint}/eclipses`, {
      params: {
        year,
        latitude: latitude ?? 12.9716,
        longitude: longitude ?? 77.5946,
        timezone: timezone || 'Asia/Kolkata',
        _: Date.now(),
      },
    });
  }

  /**
   * Get Panchang for a specific date
   */
  getPanchang(date: Date): Observable<ApiResponse<Panchang>> {
    return this.get<Panchang>(`${this.endpoint}/${this.formatDate(date)}`);
  }

  /**
   * Get Panchang for a date range
   */
  getPanchangRange(
    startDate: Date,
    endDate: Date
  ): Observable<ApiResponse<Panchang[]>> {
    return this.get<Panchang[]>(`${this.endpoint}/range`, {
      params: { startDate: this.formatDate(startDate), endDate: this.formatDate(endDate) },
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

  private formatDate(date: Date): string {
    const year = date.getFullYear();
    const month = `${date.getMonth() + 1}`.padStart(2, '0');
    const day = `${date.getDate()}`.padStart(2, '0');
    return `${year}-${month}-${day}`;
  }
}


