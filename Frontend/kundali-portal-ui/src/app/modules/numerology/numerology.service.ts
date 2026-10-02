import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { apiEndpoints } from '../../config/api-endpoints';
import { environment } from '../../config/environment/environment';

export interface NumerologyNumberDetails {
  summary: string;
  traits: string[];
  strengths: string[];
  challenges: string[];
  favourableNumbers: number[];
  favourableDays: string[];
  favourableColours: string[];
}
export interface NumerologyResponse {
  dateOfBirth: string;
  mulank: number;
  bhagyank: number;
  mulankPlanet: string;
  bhagyankPlanet: string;
  mulankCalculation: string;
  bhagyankCalculation: string;
  combinationType: string;
  combinationSummary: string;
  combinationStrengths: string[];
  combinationChallenges: string[];
  mulankDetails?: NumerologyNumberDetails | null;
  bhagyankDetails?: NumerologyNumberDetails | null;
}

@Injectable({ providedIn: 'root' })
export class NumerologyService {
  private readonly http = inject(HttpClient);
  private readonly calculateUrl = `${environment.apiBaseUrl}/${apiEndpoints.numerology.calculate}`;

  calculate(dateOfBirth: string): Observable<NumerologyResponse> {
    return this.http.post<NumerologyResponse>(this.calculateUrl, { dateOfBirth });
  }
}