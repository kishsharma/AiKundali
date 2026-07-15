import { Injectable, inject } from '@angular/core';
import { Observable, catchError, throwError } from 'rxjs';
import analysisMock from '../mockdata/mahadasha-analysis.mock.json';
import { DashaApiService } from '../../../core/services/dasha-api.service';
import { LatestKundaliStorageService } from '../../../core/services/latest-kundali-storage.service';
import { environment } from '../../../config/environment/environment';
import { MahadashaAnalysis } from '../models/mahadasha.model';

export class MissingLatestKundaliPayloadError extends Error {
  constructor() {
    super('Generate a Kundali first to view Mahadasha analysis.');
    this.name = 'MissingLatestKundaliPayloadError';
  }
}

@Injectable({
  providedIn: 'root',
})
export class MahadashaService {
  private readonly dashaApiService = inject(DashaApiService);
  private readonly latestKundaliStorage = inject(LatestKundaliStorageService);

  getAnalysis(): Observable<MahadashaAnalysis> {
    const payload = this.latestKundaliStorage.getLatest();

    if (!payload) {
      return throwError(() => new MissingLatestKundaliPayloadError());
    }

    return this.dashaApiService.getMahadashaAnalysis(payload).pipe(
      catchError((error) => {
        if (!environment.production) {
          console.warn('Mahadasha API unavailable. Falling back to mock analysis.', error);
          return [analysisMock as MahadashaAnalysis];
        }

        return throwError(() => error);
      })
    );
  }
}
