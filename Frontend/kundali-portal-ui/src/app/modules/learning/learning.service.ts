import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../config/environment/environment';
import { LearningLanguage, LearningLevelManifest, LearningLevelSummary, LearningSessionResponse } from './learning.models';

@Injectable({ providedIn: 'root' })
export class LearningService {
  private readonly baseUrl = `${environment.apiBaseUrl}/learning`;

  constructor(private readonly http: HttpClient) {}

  getLevels(): Observable<LearningLevelSummary[]> {
    return this.http.get<LearningLevelSummary[]>(`${this.baseUrl}/levels`);
  }

  getLevel(levelCode: string): Observable<LearningLevelManifest> {
    return this.http.get<LearningLevelManifest>(`${this.baseUrl}/levels/${encodeURIComponent(levelCode)}`);
  }

  getSession(sessionGuid: string, language: LearningLanguage): Observable<LearningSessionResponse> {
    const params = new HttpParams().set('language', language);
    return this.http.get<LearningSessionResponse>(`${this.baseUrl}/sessions/${encodeURIComponent(sessionGuid)}`, { params });
  }
}
