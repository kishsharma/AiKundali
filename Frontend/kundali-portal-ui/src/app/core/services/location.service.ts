import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable, catchError, map, of } from 'rxjs';
import { environment } from '../../config/environment/environment';

export interface LocationSearchResult {
  id: string;
  name: string;
  displayName: string;
  latitude: number;
  longitude: number;
  city?: string | null;
  state?: string | null;
  country?: string | null;
  countryCode?: string | null;
  timezone?: string | null;
}

interface LocationSearchResponse {
  success: boolean;
  data: LocationSearchResult[];
  message?: string;
}

@Injectable({
  providedIn: 'root'
})
export class LocationService {
  private readonly http = inject(HttpClient);
  private readonly searchUrl = `${environment.apiBaseUrl}/locations/search`;

  searchLocation(query: string, countryCode = 'IN', limit = 5): Observable<LocationSearchResult[]> {
    const normalizedQuery = query.trim();
    if (normalizedQuery.length < 3) {
      return of([]);
    }

    const params = new HttpParams()
      .set('query', normalizedQuery)
      .set('countryCode', countryCode)
      .set('limit', Math.min(Math.max(limit, 1), 10));

    return this.http
      .get<LocationSearchResponse>(this.searchUrl, { params })
      .pipe(
        map(response => response.data || []),
        catchError(() => of([]))
      );
  }
}
