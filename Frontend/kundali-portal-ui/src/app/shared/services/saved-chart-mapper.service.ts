import { Injectable } from '@angular/core';

import { GenerateKundaliRequest } from '../../core/services/kundali-api.service';
import { MatchRequestPerson } from '../../core/services/compatibility-api.service';

export interface SavedKundaliListItem {
  id: string;
  displayName: string;
  fullName: string;
  dateOfBirth: string;
  timeOfBirth: string;
  birthPlace: string;
  createdAt: string;
}

export interface SavedKundaliDetail extends SavedKundaliListItem {
  gender?: string | null;
  latitude?: number | string | null;
  longitude?: number | string | null;
  timeZone?: string | null;
  chartData?: unknown;
}

@Injectable({ providedIn: 'root' })
export class SavedChartMapperService {
  toGenerateKundaliRequest(chart: SavedKundaliDetail): GenerateKundaliRequest {
    return {
      name: chart.fullName || chart.displayName || 'Saved Kundali',
      gender: chart.gender ?? undefined,
      birthDateTime: this.toBirthDateTime(chart),
      birthPlace: chart.birthPlace || '',
      birthplace: chart.birthPlace || '',
      latitude: this.toNumber(chart.latitude),
      longitude: this.toNumber(chart.longitude),
      timeZoneOffset: this.toTimeZoneOffset(chart.timeZone),
    };
  }

  toMatchPerson(chart: SavedKundaliDetail, fallbackGender: 'Male' | 'Female'): MatchRequestPerson {
    return {
      name: chart.fullName || chart.displayName || 'Saved Kundali',
      gender: chart.gender || fallbackGender,
      birthDateTime: this.toBirthDateTime(chart),
      birthPlace: chart.birthPlace || '',
      latitude: this.toNumber(chart.latitude),
      longitude: this.toNumber(chart.longitude),
      timeZoneOffset: this.toTimeZoneOffset(chart.timeZone),
    };
  }

  toDateInput(chart: SavedKundaliDetail): string {
    return String(chart.dateOfBirth ?? '').slice(0, 10);
  }

  toTimeInput(chart: SavedKundaliDetail): string {
    return String(chart.timeOfBirth ?? '').slice(0, 5);
  }

  private toBirthDateTime(chart: SavedKundaliDetail): string {
    const date = this.toDateInput(chart);
    const time = this.toTimeInput(chart) || '00:00';
    return `${date}T${time}:00`;
  }

  private toNumber(value: number | string | null | undefined): number {
    const number = Number(value ?? 0);
    return Number.isFinite(number) ? number : 0;
  }

  private toTimeZoneOffset(value: string | null | undefined): number {
    const parsed = Number(value);
    return Number.isFinite(parsed) ? parsed : 5.5;
  }
}
