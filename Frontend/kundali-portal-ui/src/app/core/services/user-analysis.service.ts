import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../config/environment/environment';

export interface UserAnalysisPlanTile {
  code: string;
  name: string;
  userCount: number;
}

export interface UserAnalysisRow {
  id: string;
  fullName: string;
  email?: string | null;
  preferredLanguage?: string | null;
  planCode: string;
  isActive: boolean;
  chartsUsed: number;
  chartsLimit: number;
  matchesUsed: number;
  matchesLimit: number;
  questionsUsedToday: number;
  questionsDailyLimit: number | null;
  aiQuestions: number;
  nonAiQuestions: number;
  hindiQuestions: number;
  englishQuestions: number;
  otherLanguageQuestions: number;
  totalTokens: number;
}

export interface UserAnalysisResponse {
  plans: UserAnalysisPlanTile[];
  users: UserAnalysisRow[];
}

@Injectable({ providedIn: 'root' })
export class UserAnalysisService {
  private readonly http = inject(HttpClient);

  getAnalysis(): Observable<UserAnalysisResponse> {
    return this.http.get<UserAnalysisResponse>(`${environment.apiBaseUrl}/admin/user-analysis`);
  }
}
