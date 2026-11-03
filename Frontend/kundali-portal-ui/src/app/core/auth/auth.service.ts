import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, catchError, finalize, map, of, shareReplay, tap } from 'rxjs';
import { apiEndpoints } from '../../config/api-endpoints';
import { environment } from '../../config/environment/environment';
import { LatestKundaliStorageService } from '../services/latest-kundali-storage.service';
import { DhruvaKundaliContextService } from '../../shared/components/dhruva-assistant/dhruva-kundali-context.service';
import {
  AuthResponse,
  ChangePasswordRequest,
  CompleteOtpRegistrationRequest,
  LoginRequest,
  OtpChallengeResponse,
  RegisterRequest,
  StartEmailOtpRequest,
  PracticeProfile,
  UpdatePhoneNumberRequest,
  UpdatePhoneNumberResponse,
  UpdatePracticeProfileRequest,
  UpdatePreferredLanguageRequest,
  UpdatePreferredLanguageResponse,
  VerifyEmailOtpRequest,
  UserProfileResponse,
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
  private accessToken: string | null = null;
  private refreshInFlight: Observable<AuthResponse> | null = null;

  private readonly dhruvaContext = inject(DhruvaKundaliContextService);
  private readonly latestKundali = inject(LatestKundaliStorageService);

  constructor(private http: HttpClient) {}

  login(request: LoginRequest): Observable<AuthResponse> {
    return this.http.post<AuthResponse>(`${this.apiUrl}/login`, request, { withCredentials: true })
      .pipe(
        tap(response => this.saveSession(response))
      );

  }

  loginWithGoogle(idToken: string, rememberMe = false): Observable<AuthResponse> {
    return this.http.post<AuthResponse>(`${this.apiUrl}/google`, { idToken, rememberMe }, { withCredentials: true })
      .pipe(
        tap(response => this.saveSession(response))
      );
  }

  register(request: RegisterRequest): Observable<AuthResponse> {
    return this.http.post<AuthResponse>(`${this.apiUrl}/register`, request, { withCredentials: true })
      .pipe(
        tap(response => this.saveSession(response))
      );
  }

  startOtpLogin(request: StartEmailOtpRequest): Observable<OtpChallengeResponse> {
    return this.http.post<OtpChallengeResponse>(`${this.apiUrl}/otp/start-login`, request);
  }

  verifyOtpLogin(request: VerifyEmailOtpRequest): Observable<AuthResponse> {
    return this.http.post<AuthResponse>(`${this.apiUrl}/otp/verify-login`, request, { withCredentials: true })
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
    return this.http.post<AuthResponse>(`${this.apiUrl}/otp/complete-registration`, request, { withCredentials: true })
      .pipe(
        tap(response => this.saveSession(response))
      );
  }

  changePassword(request: ChangePasswordRequest): Observable<{ message: string }> {
    return this.http.post<{ message: string }>(`${this.apiUrl}/change-password`, request, { withCredentials: true });
  }

  updatePreferredLanguage(request: UpdatePreferredLanguageRequest): Observable<UpdatePreferredLanguageResponse> {
    return this.http.patch<UpdatePreferredLanguageResponse>(`${this.userApiUrl}/me/preferred-language`, request)
      .pipe(
        tap((response) => {
          this.updateCachedUser({ preferredLanguage: response.preferredLanguage });
        })
      );
  }

  private saveSession(response: AuthResponse): void {
    const previousUserId = this.getCurrentUser()?.userId ?? null;
    this.accessToken = response.token;
    localStorage.removeItem(this.tokenKey);
    const { token: _token, ...profile } = response;
    localStorage.setItem(this.userKey, JSON.stringify(profile));
    if (previousUserId !== (response.userId ?? null)) {
      this.clearSelectedChart();
    }
  }

  getToken(): string | null {
    return this.accessToken ?? localStorage.getItem(this.tokenKey);
  }

  getCurrentUser(): AuthResponse | null {
    const user = localStorage.getItem(this.userKey);
    if (!user) {
      return null;
    }

    return { ...JSON.parse(user), token: this.getToken() ?? '' } as AuthResponse;
  }

  isLoggedIn(): boolean {
    return !!this.getToken();
  }

  restoreSession(): Observable<boolean> {
    if (this.getToken()) {
      return of(true);
    }

    return this.refreshSession().pipe(
      map(() => true),
      catchError(() => {
        this.clearSession();
        return of(false);
      })
    );
  }

  refreshSession(): Observable<AuthResponse> {
    if (this.refreshInFlight) {
      return this.refreshInFlight;
    }

    this.refreshInFlight = this.http.post<AuthResponse>(`${this.apiUrl}/refresh`, {}, { withCredentials: true })
      .pipe(
        tap(response => this.saveSession(response)),
        finalize(() => this.refreshInFlight = null),
        shareReplay({ bufferSize: 1, refCount: false })
      );
    return this.refreshInFlight;
  }

  logout(): void {
    this.http.post<void>(`${this.apiUrl}/logout`, {}, { withCredentials: true }).subscribe({
      error: () => undefined
    });
    this.clearSession();
  }

  clearSession(): void {
    this.accessToken = null;
    localStorage.removeItem(this.tokenKey);
    localStorage.removeItem(this.userKey);
    this.clearSelectedChart();
  }

  private clearSelectedChart(): void {
    this.dhruvaContext.clearContext();
    this.latestKundali.clear();
  }

  updatePhone(request: UpdatePhoneNumberRequest): Observable<UpdatePhoneNumberResponse> {
    return this.http.patch<UpdatePhoneNumberResponse>(`${this.userApiUrl}/me/phone`, request)
      .pipe(
        tap((response) => this.updateCachedUser({ phoneNumber: response.phoneNumber }))
      );
  }

  getPractice(): Observable<PracticeProfile> {
    return this.http.get<PracticeProfile>(`${this.userApiUrl}/me/practice`);
  }

  savePractice(request: UpdatePracticeProfileRequest): Observable<PracticeProfile> {
    return this.http.put<PracticeProfile>(`${this.userApiUrl}/me/practice`, request);
  }

  getMe(): Observable<UserProfileResponse> {
    return this.http.get<UserProfileResponse>(`${this.userApiUrl}/me`)
      .pipe(
        tap((profile) => {
          this.updateCachedUser(profile);
        })
      );
  }

  issueStoreHandoff(): Observable<{ code: string; expiresAt: string }> {
    return this.http.post<{ code: string; expiresAt: string }>(`${this.apiUrl}/store-handoff`, {});
  }

  private updateCachedUser(update: Partial<AuthResponse> | UserProfileResponse): void {
    const currentUser = this.getCurrentUser();
    if (!currentUser) {
      return;
    }

    const nextUser: AuthResponse = {
      ...currentUser,
      ...update,
      token: currentUser.token
    };

    const { token: _token, ...profile } = nextUser;
    localStorage.setItem(this.userKey, JSON.stringify(profile));
  }
}
