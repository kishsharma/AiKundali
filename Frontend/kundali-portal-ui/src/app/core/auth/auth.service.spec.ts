import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { apiEndpoints } from '../../config/api-endpoints';
import { environment } from '../../config/environment/environment';
import { AuthResponse, UserProfileResponse } from './auth.models';
import { AuthService } from './auth.service';
import { DhruvaKundaliContextService } from '../../shared/components/dhruva-assistant/dhruva-kundali-context.service';

describe('AuthService', () => {
  let service: AuthService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    localStorage.clear();

    TestBed.configureTestingModule({
      providers: [
        AuthService,
        provideHttpClient(),
        provideHttpClientTesting()
      ]
    });

    service = TestBed.inject(AuthService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
    localStorage.clear();
  });

  it('drops another user chart when the session ends or a different user signs in', () => {
    const dhruva = TestBed.inject(DhruvaKundaliContextService);
    dhruva.setContext({ personName: { value: 'Aditi', status: 'available' } } as never);
    localStorage.setItem('kundali.latestBirthPayload', JSON.stringify({ name: 'Aditi' }));
    localStorage.setItem('kundali_user', JSON.stringify({ userId: 'user-a', token: 'old' }));

    service.clearSession();

    expect(dhruva.context()).toBeNull();
    expect(dhruva.source()).toBeNull();
    expect(localStorage.getItem('kundali.latestBirthPayload')).toBeNull();

    dhruva.setContext({ personName: { value: 'Aditi', status: 'available' } } as never);
    localStorage.setItem('kundali_user', JSON.stringify({ userId: 'user-a', token: 'old' }));
    service.login({ email: 'b@example.com', password: 'secret' }).subscribe();
    httpMock.expectOne(`${environment.apiBaseUrl}/${apiEndpoints.auth}/login`).flush({
      userId: 'user-b',
      fullName: 'User B',
      role: 'User',
      token: 'token-b',
    });

    expect(dhruva.context()).toBeNull();
  });

  it('updates cached user preferred language after PATCH', () => {
    const cachedUser: AuthResponse = {
      userId: 'u1',
      fullName: 'Test User',
      email: 'test@example.com',
      role: 'User',
      preferredLanguage: 'en',
      token: 'token-123'
    };
    localStorage.setItem('kundali_user', JSON.stringify(cachedUser));
    localStorage.setItem('kundali_token', cachedUser.token);

    service.updatePreferredLanguage({ preferredLanguage: 'hi' }).subscribe((response) => {
      expect(response.preferredLanguage).toBe('hi');
    });

    const req = httpMock.expectOne(`${environment.apiBaseUrl}/${apiEndpoints.user}/me/preferred-language`);
    expect(req.request.method).toBe('PATCH');
    expect(req.request.body).toEqual({ preferredLanguage: 'hi' });
    req.flush({ preferredLanguage: 'hi' });

    const updatedUser = service.getCurrentUser();
    expect(updatedUser?.preferredLanguage).toBe('hi');
    expect(updatedUser?.token).toBe('token-123');
  });

  it('updates cached user details after getMe while preserving token', () => {
    const cachedUser: AuthResponse = {
      userId: 'u1',
      fullName: 'Old Name',
      email: 'old@example.com',
      role: 'User',
      preferredLanguage: 'en',
      token: 'token-123'
    };
    const profile: UserProfileResponse = {
      userId: 'u1',
      fullName: 'Updated Name',
      email: 'updated@example.com',
      role: 'User',
      preferredLanguage: 'kn'
    };

    localStorage.setItem('kundali_user', JSON.stringify(cachedUser));
    localStorage.setItem('kundali_token', cachedUser.token);

    service.getMe().subscribe((response) => {
      expect(response.fullName).toBe('Updated Name');
      expect(response.preferredLanguage).toBe('kn');
    });

    const req = httpMock.expectOne(`${environment.apiBaseUrl}/${apiEndpoints.user}/me`);
    expect(req.request.method).toBe('GET');
    req.flush(profile);

    const updatedUser = service.getCurrentUser();
    expect(updatedUser?.fullName).toBe('Updated Name');
    expect(updatedUser?.email).toBe('updated@example.com');
    expect(updatedUser?.preferredLanguage).toBe('kn');
    expect(updatedUser?.token).toBe('token-123');
  });
});
