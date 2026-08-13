import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { apiEndpoints } from '../../config/api-endpoints';
import { environment } from '../../config/environment/environment';

export interface GocharDailyRequest {
  date: Date;
  latitude?: number;
  longitude?: number;
  timezone?: string;
}

export interface GocharPlanet {
  planet: string;
  planetName: string;
  sign: string;
  signName: string;
  house: number;
  degree: number;
  minute: number;
  degreeInSign: number;
  longitude: number;
  nakshatra: string;
  pada: number;
  isRetrograde: boolean;
}

export interface GocharTransition {
  planet: string;
  fromSign: string;
  toSign: string;
  ingressDate: string;
  ingressTime: string;
  isRetrograde: boolean;
}

export interface GocharDailyResponse {
  date: string;
  calculatedAtUtc: string;
  latitude: number;
  longitude: number;
  timezone: string;
  ascendant: string;
  planets: GocharPlanet[];
  transitions: GocharTransition[];
}

@Injectable({ providedIn: 'root' })
export class GocharApiService {
  private readonly endpoint = `${environment.apiBaseUrl}/${apiEndpoints.gochar.root}`;

  constructor(private readonly http: HttpClient) {}

  getDailyGochar(request: GocharDailyRequest): Observable<GocharDailyResponse> {
    return this.http.get<GocharDailyResponse>(`${this.endpoint}/daily`, {
      params: {
        date: this.formatDate(request.date),
        latitude: request.latitude ?? 12.9716,
        longitude: request.longitude ?? 77.5946,
        timezone: request.timezone || 'Asia/Kolkata',
      },
    });
  }

  private formatDate(date: Date): string {
    const year = date.getFullYear();
    const month = `${date.getMonth() + 1}`.padStart(2, '0');
    const day = `${date.getDate()}`.padStart(2, '0');
    return `${year}-${month}-${day}`;
  }
}
