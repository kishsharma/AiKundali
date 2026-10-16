import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../config/environment/environment';
import { apiEndpoints } from '../../config/api-endpoints';
import {
  HoroscopeHorizon,
  HoroscopePeriodRequest,
  HoroscopePeriodResponse,
  HoroscopeRashifalResponse
} from './horoscope.models';

@Injectable({ providedIn: 'root' })
export class HoroscopeService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiBaseUrl}/${apiEndpoints.horoscope.root}`;

  getPeriod(request: HoroscopePeriodRequest): Observable<HoroscopePeriodResponse> {
    const horizon = this.normalizeHorizon(request.horizon);
    const path = horizon === 'Weekly' ? 'weekly' : horizon === 'Monthly' ? 'monthly' : 'daily';
    return this.http.post<HoroscopePeriodResponse>(`${this.baseUrl}/${path}`, request);
  }

  getRashifal(horizon: HoroscopeHorizon, anchorDate: string): Observable<HoroscopeRashifalResponse> {
    const path = this.normalizeHorizon(horizon).toLowerCase();
    return this.http.get<HoroscopeRashifalResponse>(`${this.baseUrl}/rashifal/${path}`, {
      params: new HttpParams().set('anchorDate', anchorDate)
    });
  }

  normalizeHorizon(horizon?: HoroscopeHorizon): 'Daily' | 'Weekly' | 'Monthly' {
    if (horizon === 1 || horizon === 'Weekly') {
      return 'Weekly';
    }
    if (horizon === 2 || horizon === 'Monthly') {
      return 'Monthly';
    }
    return 'Daily';
  }
}
