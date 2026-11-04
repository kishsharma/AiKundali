import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { timeout } from 'rxjs/operators';

import { environment } from '../../config/environment/environment';

export type DhruvaNarrationMode = 'janma_story' | 'dasha_focus' | 'compatibility' | 'qa';

export interface DhruvaNarrateRequest {
  mode: DhruvaNarrationMode;
  question?: string;
  locale?: string;
  tone?: 'friendly' | 'astrologer' | 'corporate';
  kundaliId?: string | null;
  chart: unknown;
}

export interface DhruvaChatChart {
  kundaliId: string;
  fullName: string;
  dateOfBirth: string;
  birthPlace: string;
  messageCount: number;
  lastAskedAt: string;
}

export interface DhruvaChatMessage {
  id: string;
  question: string;
  response: string;
  askedAt: string;
}

export interface DhruvaNarrateResponse {
  narration: string;
  isAiGenerated?: boolean;
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

  saveChat(request: { kundaliId: string; question: string; response: string }): Observable<void> {
    return this.http.post<void>(`${environment.apiBaseUrl}/dhruva/chats`, request);
  }

  listChatCharts(): Observable<DhruvaChatChart[]> {
    return this.http.get<DhruvaChatChart[]>(`${environment.apiBaseUrl}/dhruva/chats`);
  }

  listChatMessages(kundaliId: string): Observable<DhruvaChatMessage[]> {
    return this.http.get<DhruvaChatMessage[]>(`${environment.apiBaseUrl}/dhruva/chats/${kundaliId}`);
  }
}
