import { HttpClient, provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { Router } from '@angular/router';
import { AuthService } from './auth.service';
import { authInterceptor } from './auth.interceptor';

describe('authInterceptor', () => {
  let httpMock: HttpTestingController;
  let authService: AuthService;
  let router: { navigate: ReturnType<typeof vi.fn> };

  beforeEach(() => {
    localStorage.clear();
    router = { navigate: vi.fn() };
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(withInterceptors([authInterceptor])),
        provideHttpClientTesting(),
        AuthService,
        { provide: Router, useValue: router }
      ]
    });
    httpMock = TestBed.inject(HttpTestingController);
    authService = TestBed.inject(AuthService);
  });

  afterEach(() => {
    httpMock.verify();
    localStorage.clear();
  });

  it('refreshes once and retries an expired bearer request', () => {
    localStorage.setItem('kundali_token', 'expired-token');
    let result: unknown;

    const http = TestBed.inject(HttpClient);
    http.get('/api/private').subscribe(value => result = value);

    const initial = httpMock.expectOne('/api/private');
    expect(initial.request.headers.get('Authorization')).toBe('Bearer expired-token');
    initial.flush({}, { status: 401, statusText: 'Unauthorized' });

    const refresh = httpMock.expectOne(request => request.url.endsWith('/auth/refresh'));
    expect(refresh.request.withCredentials).toBe(true);
    refresh.flush({ userId: 'u1', fullName: 'User', role: 'User', token: 'new-token' });

    const retry = httpMock.expectOne('/api/private');
    expect(retry.request.headers.get('Authorization')).toBe('Bearer new-token');
    retry.flush({ ok: true });

    expect(result).toEqual({ ok: true });
    expect(authService.getToken()).toBe('new-token');
    expect(router.navigate).not.toHaveBeenCalled();
  });

  it('shares one refresh across simultaneous expired requests', () => {
    localStorage.setItem('kundali_token', 'expired-token');
    const http = TestBed.inject(HttpClient);
    http.get('/api/one').subscribe();
    http.get('/api/two').subscribe();

    httpMock.expectOne('/api/one').flush({}, { status: 401, statusText: 'Unauthorized' });
    httpMock.expectOne('/api/two').flush({}, { status: 401, statusText: 'Unauthorized' });
    const refreshes = httpMock.match(request => request.url.endsWith('/auth/refresh'));
    expect(refreshes).toHaveLength(1);
    refreshes[0].flush({ userId: 'u1', fullName: 'User', role: 'User', token: 'new-token' });

    const retries = httpMock.match(request => request.url === '/api/one' || request.url === '/api/two');
    expect(retries).toHaveLength(2);
    retries.forEach(request => request.flush({ ok: true }));
  });
});
