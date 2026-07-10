import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { BaseApiService } from './base-api.service';

export interface AstrologerBrandingSetting {
  id?: string;
  userId?: string;
  astrologerName: string;
  designation?: string | null;
  address?: string | null;
  contactNumber?: string | null;
  email?: string | null;
  logoUrl?: string | null;
  showOnChartPage: boolean;
  showOnCompatibilityPage: boolean;
  showOnPdfReport: boolean;
  isActive: boolean;
  createdAt?: string;
  updatedAt?: string | null;
}

@Injectable({
  providedIn: 'root'
})
export class AstrologerBrandingSettingsService extends BaseApiService {
  getMine(): Observable<AstrologerBrandingSetting | null> {
    return this.get<AstrologerBrandingSetting | null>('AstrologerBrandingSettings/me');
  }

  saveMine(setting: AstrologerBrandingSetting): Observable<AstrologerBrandingSetting> {
    return this.put<AstrologerBrandingSetting>('AstrologerBrandingSettings/me', setting);
  }
}
