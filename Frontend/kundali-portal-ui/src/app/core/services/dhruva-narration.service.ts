import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { timeout } from 'rxjs/operators';

import { environment } from '../../config/environment/environment';

export type DhruvaNarrationMode = 'janma_story' | 'dasha_focus' | 'qa';

export interface DhruvaNarrateRequest {
  mode: DhruvaNarrationMode;
  question?: string;
  locale?: string;
  tone?: 'friendly' | 'astrologer' | 'corporate';
  chart: unknown;
}

export interface DhruvaNarrateResponse {
  narration: string;
  model: string;
  promptVersion: string;
  disclaimer: string;
}

export interface DhruvaAllowance {
  dhruvaAiEnabled: boolean;
  lifeQuestionsRemaining: number | null;
  lifeQuestionDailyLimit: number | null;
}

@Injectable({
  providedIn: 'root',
})
export class DhruvaNarrationService {
  private readonly narrationTimeoutMs = 180000;

  constructor(private readonly http: HttpClient) {}

  narrate(request: DhruvaNarrateRequest): Observable<DhruvaNarrateResponse> {
    return this.http
      .post<DhruvaNarrateResponse>(`${environment.apiBaseUrl}/dhruva/narrate`, request)
      .pipe(timeout(this.narrationTimeoutMs));
  }

  getAllowance(): Observable<DhruvaAllowance> {
    return this.http.get<DhruvaAllowance>(`${environment.apiBaseUrl}/dhruva/allowance`);
  }
}
