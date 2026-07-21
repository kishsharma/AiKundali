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
}
