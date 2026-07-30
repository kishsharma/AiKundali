import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, tap } from 'rxjs';
import { apiEndpoints } from '../../config/api-endpoints';
import { environment } from '../../config/environment/environment';
import {
  AuthResponse,
  ChangePasswordRequest,
  CompleteOtpRegistrationRequest,
  LoginRequest,
  OtpChallengeResponse,
  RegisterRequest,
  StartEmailOtpRequest,
  VerifyEmailOtpRequest,
  VerifyRegistrationOtpResponse
} from './auth.models';

@Injectable({
  providedIn: 'root'
})
export class AuthService {
  private readonly apiUrl = `${environment.apiBaseUrl}/${apiEndpoints.auth}`;
  private readonly userApiUrl = `${environment.apiBaseUrl}/${apiEndpoints.user}`;

  private readonly tokenKey = 'kundali_token';
  private readonly userKey = 'kundali_user';

  constructor(private http: HttpClient) {}

  login(request: LoginRequest): Observable<AuthResponse> {
    return this.http.post<AuthResponse>(`${this.apiUrl}/login`, request)
      .pipe(
        tap(response => this.saveSession(response))
      );

  }

  register(request: RegisterRequest): Observable<AuthResponse> {
    return this.http.post<AuthResponse>(`${this.apiUrl}/register`, request)
      .pipe(
        tap(response => this.saveSession(response))
      );
  }

  startOtpLogin(request: StartEmailOtpRequest): Observable<OtpChallengeResponse> {
    return this.http.post<OtpChallengeResponse>(`${this.apiUrl}/otp/start-login`, request);
  }

  verifyOtpLogin(request: VerifyEmailOtpRequest): Observable<AuthResponse> {
    return this.http.post<AuthResponse>(`${this.apiUrl}/otp/verify-login`, request)
      .pipe(
        tap(response => this.saveSession(response))
      );
  }

  startOtpRegistration(request: StartEmailOtpRequest): Observable<OtpChallengeResponse> {
    return this.http.post<OtpChallengeResponse>(`${this.apiUrl}/otp/start-registration`, request);
  }

  verifyOtpRegistration(request: VerifyEmailOtpRequest): Observable<VerifyRegistrationOtpResponse> {
    return this.http.post<VerifyRegistrationOtpResponse>(`${this.apiUrl}/otp/verify-registration`, request);
  }

  completeOtpRegistration(request: CompleteOtpRegistrationRequest): Observable<AuthResponse> {
    return this.http.post<AuthResponse>(`${this.apiUrl}/otp/complete-registration`, request)
      .pipe(
        tap(response => this.saveSession(response))
      );
  }

  changePassword(request: ChangePasswordRequest): Observable<{ message: string }> {
    return this.http.post<{ message: string }>(`${this.apiUrl}/change-password`, request);
  }

  private saveSession(response: AuthResponse): void {
    localStorage.setItem(this.tokenKey, response.token);
    localStorage.setItem(this.userKey, JSON.stringify(response));
  }

  getToken(): string | null {
    return localStorage.getItem(this.tokenKey);
  }

  getCurrentUser(): AuthResponse | null {
    const user = localStorage.getItem(this.userKey);
    return user ? JSON.parse(user) : null;
  }

  isLoggedIn(): boolean {
    return !!this.getToken();
  }

  logout(): void {
    this.clearSession();
  }

  clearSession(): void {
    localStorage.removeItem(this.tokenKey);
    localStorage.removeItem(this.userKey);
  }

  getMe() {
    return this.http.get<any>(`${this.userApiUrl}/me`);
  }
}
