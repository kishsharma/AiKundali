import { Injectable, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, tap } from 'rxjs';
import { environment } from '../../config/environment';
import {
  AuthResponse,
  CompleteOtpRegistrationRequest,
  LoginRequest,
  OtpChallengeResponse,
  RegisterRequest,
  StartEmailOtpRequest,
  VerifyEmailOtpRequest,
  VerifyRegistrationOtpResponse
} from './auth.models';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly apiUrl = `${environment.apiBaseUrl}/auth`;
  private readonly tokenKey = 'horalife_store_token';
  private readonly userKey = 'horalife_store_user';
  readonly session = signal(0);

  constructor(private readonly http: HttpClient) {}

  login(request: LoginRequest): Observable<AuthResponse> {
    return this.http.post<AuthResponse>(`${this.apiUrl}/login`, request).pipe(
      tap((response) => this.saveSession(response))
    );
  }

  register(request: RegisterRequest): Observable<AuthResponse> {
    return this.http.post<AuthResponse>(`${this.apiUrl}/register`, request).pipe(
      tap((response) => this.saveSession(response))
    );
  }

  startOtpLogin(request: StartEmailOtpRequest): Observable<OtpChallengeResponse> {
    return this.http.post<OtpChallengeResponse>(`${this.apiUrl}/otp/start-login`, request);
  }

  verifyOtpLogin(request: VerifyEmailOtpRequest): Observable<AuthResponse> {
    return this.http.post<AuthResponse>(`${this.apiUrl}/otp/verify-login`, request).pipe(
      tap((response) => this.saveSession(response))
    );
  }

  startOtpRegistration(request: StartEmailOtpRequest): Observable<OtpChallengeResponse> {
    return this.http.post<OtpChallengeResponse>(`${this.apiUrl}/otp/start-registration`, request);
  }

  verifyOtpRegistration(request: VerifyEmailOtpRequest): Observable<VerifyRegistrationOtpResponse> {
    return this.http.post<VerifyRegistrationOtpResponse>(`${this.apiUrl}/otp/verify-registration`, request);
  }

  redeemStoreHandoff(code: string): Observable<AuthResponse> {
    return this.http.post<AuthResponse>(`${this.apiUrl}/store-handoff/redeem`, { code }).pipe(
      tap((response) => this.saveSession(response))
    );
  }

  completeOtpRegistration(request: CompleteOtpRegistrationRequest): Observable<AuthResponse> {
    return this.http.post<AuthResponse>(`${this.apiUrl}/otp/complete-registration`, request).pipe(
      tap((response) => this.saveSession(response))
    );
  }

  getToken(): string | null {
    return localStorage.getItem(this.tokenKey);
  }

  getCurrentUser(): AuthResponse | null {
    const user = localStorage.getItem(this.userKey);
    return user ? (JSON.parse(user) as AuthResponse) : null;
  }

  isLoggedIn(): boolean {
    return !!this.getToken();
  }

  clearSession(): void {
    localStorage.removeItem(this.tokenKey);
    localStorage.removeItem(this.userKey);
    this.session.update((value) => value + 1);
  }

  private saveSession(response: AuthResponse): void {
    localStorage.setItem(this.tokenKey, response.token);
    localStorage.setItem(this.userKey, JSON.stringify(response));
    this.session.update((value) => value + 1);
  }
}
