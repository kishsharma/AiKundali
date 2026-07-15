import { Injectable } from '@angular/core';
import { GenerateKundaliRequest } from './kundali-api.service';

const LATEST_KUNDALI_PAYLOAD_KEY = 'kundali.latestBirthPayload';

@Injectable({
  providedIn: 'root',
})
export class LatestKundaliStorageService {
  save(payload: GenerateKundaliRequest): void {
    if (!this.isBrowserStorageAvailable()) {
      return;
    }

    window.localStorage.setItem(LATEST_KUNDALI_PAYLOAD_KEY, JSON.stringify(payload));
  }

  getLatest(): GenerateKundaliRequest | null {
    if (!this.isBrowserStorageAvailable()) {
      return null;
    }

    const raw = window.localStorage.getItem(LATEST_KUNDALI_PAYLOAD_KEY);

    if (!raw) {
      return null;
    }

    try {
      const payload = JSON.parse(raw) as GenerateKundaliRequest;
      return this.isValid(payload) ? payload : null;
    } catch {
      this.clear();
      return null;
    }
  }

  clear(): void {
    if (!this.isBrowserStorageAvailable()) {
      return;
    }

    window.localStorage.removeItem(LATEST_KUNDALI_PAYLOAD_KEY);
  }

  private isValid(payload: GenerateKundaliRequest | null): payload is GenerateKundaliRequest {
    return !!payload
      && typeof payload.name === 'string'
      && typeof payload.birthDateTime === 'string'
      && typeof payload.latitude === 'number'
      && typeof payload.longitude === 'number';
  }

  private isBrowserStorageAvailable(): boolean {
    return typeof window !== 'undefined' && typeof window.localStorage !== 'undefined';
  }
}
