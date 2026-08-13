import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { apiEndpoints } from '../../config/api-endpoints';
import { environment } from '../../config/environment/environment';

export interface UserLoginControlUser {
  id: string;
  fullName: string;
  email: string;
  phoneNumber?: string | null;
  isActive: boolean;
  kundaliSaveLimit: number;
  planCode: string;
  planExpiresAt?: string | null;
  isPremium: boolean;
}

@Injectable({
  providedIn: 'root'
})
export class UserLoginControlService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = `${environment.apiBaseUrl}/${apiEndpoints.user}`;

  getUsers(): Observable<UserLoginControlUser[]> {
    return this.http.get<UserLoginControlUser[]>(this.apiUrl);
  }

  updateActiveStatus(id: string, isActive: boolean): Observable<UserLoginControlUser> {
    return this.http.patch<UserLoginControlUser>(`${this.apiUrl}/${id}/active`, { isActive });
  }

  updateKundaliSaveLimit(id: string, kundaliSaveLimit: number): Observable<UserLoginControlUser> {
    return this.http.patch<UserLoginControlUser>(`${this.apiUrl}/${id}/kundali-save-limit`, { kundaliSaveLimit });
  }

  updatePremiumPlan(id: string, isPremium: boolean, planExpiresAt: string | null): Observable<UserLoginControlUser> {
    return this.http.patch<UserLoginControlUser>(`${this.apiUrl}/${id}/premium-plan`, { isPremium, planExpiresAt });
  }
}