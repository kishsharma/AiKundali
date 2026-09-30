import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../config/environment/environment';

export interface LifeAreaCatalogItem {
  key: string;
  name: string;
  primaryCharts: string[];
  supportingCharts: string[];
  requiredHouses: number[];
  requiredPlanets: string[];
}

export interface LifeAreaFactor {
  subject: string;
  natalSign: string;
  natalDignity: string;
  specialistChart: string;
  specialistSign: string;
  specialistDignity: string;
  tone: string;
  reading: string;
}

export interface LifeAreaPeriod {
  label: string;
  planet: string;
  tone: string;
  reading: string;
}

export interface LifeAreaReport {
  areaKey: string;
  areaName: string;
  verdict: string;
  verdictText: string;
  chartsConsulted: string[];
  specialistChart: string;
  factors: LifeAreaFactor[];
  crossValidation: string;
  timing: string;
  periods: LifeAreaPeriod[];
  note?: string | null;
  plainMeaning: string;
}

@Injectable({ providedIn: 'root' })
export class LifeAreaAnalysisService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = `${environment.apiBaseUrl}/life-area-analysis`;

  getAreas(locale: string): Observable<LifeAreaCatalogItem[]> {
    return this.http.get<LifeAreaCatalogItem[]>(`${this.apiUrl}/areas`, {
      params: new HttpParams().set('locale', locale)
    });
  }

  getReport(kundaliId: string, area: string, locale: string): Observable<LifeAreaReport> {
    return this.http.get<LifeAreaReport>(`${this.apiUrl}/${kundaliId}`, {
      params: new HttpParams().set('area', area).set('locale', locale)
    });
  }

  getInsight(kundaliId: string, area: string, locale: string): Observable<{ insight: string }> {
    return this.http.post<{ insight: string }>(`${this.apiUrl}/${kundaliId}/insight`, null, {
      params: new HttpParams().set('area', area).set('locale', locale)
    });
  }
}
