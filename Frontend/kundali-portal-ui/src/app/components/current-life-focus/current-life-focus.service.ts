import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

import { apiEndpoints } from '../../config/api-endpoints';
import { environment } from '../../config/environment/environment';
import {
  CurrentLifeFocusRequest,
  CurrentLifeFocusResponse,
  FocusedLifeAreaRequest,
  FocusedLifeAreaResponse
} from './current-life-focus.models';

@Injectable({ providedIn: 'root' })
export class CurrentLifeFocusService {
  private readonly baseUrl = `${environment.apiBaseUrl}/${apiEndpoints.gochar.currentLifeFocus}`;

  constructor(private readonly http: HttpClient) {}

  getCurrentLifeFocus(request: CurrentLifeFocusRequest): Observable<CurrentLifeFocusResponse> {
    return this.http.post<CurrentLifeFocusResponse>(this.baseUrl, request);
  }

  getFocusedLifeAreaAnalysis(
    analysisId: string,
    request: FocusedLifeAreaRequest
  ): Observable<FocusedLifeAreaResponse> {
    return this.http.post<FocusedLifeAreaResponse>(`${this.baseUrl}/${analysisId}/focus`, request);
  }
}
