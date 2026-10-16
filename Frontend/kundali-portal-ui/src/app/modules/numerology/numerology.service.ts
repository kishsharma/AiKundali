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

export interface NumerologyValueCheck {
  label: string;
  compound: number;
  number: number;
  planet: string;
  calculation: string;
  mulankMatch: string;
  bhagyankMatch: string;
  suggestions: string[];
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
  loShu?: NumerologyLoShu | null;
  personalYear?: NumerologyPersonalYear | null;
  nextPersonalYear?: NumerologyPersonalYear | null;
  name?: string | null;
  nameCompound?: number | null;
  nameNumber?: number | null;
  namePlanet?: string | null;
  nameCalculation?: string | null;
  nameMulankMatch?: string | null;
  nameBhagyankMatch?: string | null;
  mobile?: NumerologyValueCheck | null;
  business?: NumerologyValueCheck | null;
}

export interface NumerologyLoShu {
  cells: NumerologyLoShuCell[];
  missing: number[];
  repeated: NumerologyRepeat[];
}

export interface NumerologyLoShuCell {
  number: number;
  count: number;
}

export interface NumerologyRepeat {
  number: number;
  count: number;
}

export interface NumerologyPersonalYear {
  year: number;
  number: number;
  planet: string;
  calculation: string;
}

@Injectable({ providedIn: 'root' })
export class NumerologyService {
  private readonly http = inject(HttpClient);
  private readonly calculateUrl = `${environment.apiBaseUrl}/${apiEndpoints.numerology.calculate}`;

  calculate(
    dateOfBirth: string,
    name?: string,
    mobileNumber?: string,
    businessName?: string
  ): Observable<NumerologyResponse> {
    const trimmedName = name?.trim();
    const trimmedMobile = mobileNumber?.trim();
    const trimmedBusiness = businessName?.trim();
    return this.http.post<NumerologyResponse>(this.calculateUrl, {
      dateOfBirth,
      name: trimmedName ? trimmedName : null,
      mobileNumber: trimmedMobile ? trimmedMobile : null,
      businessName: trimmedBusiness ? trimmedBusiness : null
    });
  }
}
