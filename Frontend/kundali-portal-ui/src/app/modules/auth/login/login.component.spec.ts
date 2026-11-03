import { ComponentFixture, TestBed } from '@angular/core/testing';
import { convertToParamMap } from '@angular/router';
import { EMPTY, of } from 'rxjs';
import { TranslocoService } from '@jsverse/transloco';

import { LoginComponent } from './login.component';
import { AuthService } from '../../../core/auth/auth.service';
import { NotificationService } from '../../../shared/services/notification.service';
import { ActivatedRoute, Router } from '@angular/router';

describe('LoginComponent', () => {
  let component: LoginComponent;
  let fixture: ComponentFixture<LoginComponent>;
  let authServiceSpy: any;
  let routerSpy: any;
  let notificationsSpy: any;
  let translocoServiceSpy: any;

  beforeEach(async () => {
    authServiceSpy = {
      getMe: vi.fn(),
      startOtpLogin: vi.fn(),
      verifyOtpLogin: vi.fn(),
      login: vi.fn(),
      loginWithGoogle: vi.fn()
    };
    routerSpy = {
      navigate: vi.fn(),
      navigateByUrl: vi.fn()
    };
    notificationsSpy = {
      success: vi.fn(),
      error: vi.fn(),
      warning: vi.fn()
    };
    translocoServiceSpy = {
      setActiveLang: vi.fn()
    };

    await TestBed.configureTestingModule({
      imports: [LoginComponent],
      providers: [
        { provide: AuthService, useValue: authServiceSpy },
        { provide: Router, useValue: routerSpy },
        { provide: NotificationService, useValue: notificationsSpy },
        { provide: TranslocoService, useValue: translocoServiceSpy },
        {
          provide: ActivatedRoute,
          useValue: {
            snapshot: {
              queryParamMap: convertToParamMap({ returnUrl: '/app/compatibility' })
            }
          }
        }
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(LoginComponent);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('defaults the long-lived sign-in choice to off', () => {
    expect(component.rememberMe).toBe(false);
  });

  it('sends rememberMe with password login', () => {
    authServiceSpy.login.mockReturnValue(EMPTY);
    component.email = 'test@example.com';
    component.password = 'secret';
    component.rememberMe = true;

    component.login();

    expect(authServiceSpy.login).toHaveBeenCalledWith({
      email: 'test@example.com',
      password: 'secret',
      rememberMe: true
    });
  });

  it('sends rememberMe with OTP verification', () => {
    authServiceSpy.verifyOtpLogin.mockReturnValue(EMPTY);
    component.email = 'test@example.com';
    component.challengeId = 'challenge-1';
    component.otpDigits = ['1', '2', '3', '4', '5', '6'];
    component.otpCode = '123456';
    component.rememberMe = true;

    component.verifyLoginCode();

    expect(authServiceSpy.verifyOtpLogin).toHaveBeenCalledWith({
      channel: 'Email',
      email: 'test@example.com',
      challengeId: 'challenge-1',
      code: '123456',
      rememberMe: true
    });
  });

  it('sends rememberMe with Google sign-in', () => {
    authServiceSpy.loginWithGoogle.mockReturnValue(EMPTY);
    component.rememberMe = true;

    component.loginWithGoogle('google-id-token');

    expect(authServiceSpy.loginWithGoogle).toHaveBeenCalledWith('google-id-token', true);
  });

  it('redirects to required language selection when preferredLanguage is null', async () => {
    vi.useFakeTimers();
    authServiceSpy.getMe.mockReturnValue(of({
      userId: 'u1',
      fullName: 'User',
      role: 'User',
      preferredLanguage: null
    }));

    (component as any).finishLogin();
    await vi.advanceTimersByTimeAsync(260);

    expect(routerSpy.navigate).toHaveBeenCalledWith(['/select-language'], {
      queryParams: {
        returnUrl: '/app/compatibility'
      }
    });
    expect(routerSpy.navigateByUrl).not.toHaveBeenCalled();
    vi.useRealTimers();
  });

  it('applies preferredLanguage and continues to returnUrl when language exists', async () => {
    vi.useFakeTimers();
    authServiceSpy.getMe.mockReturnValue(of({
      userId: 'u1',
      fullName: 'User',
      role: 'User',
      preferredLanguage: 'hi'
    }));

    (component as any).finishLogin();
    await vi.advanceTimersByTimeAsync(260);

    expect(translocoServiceSpy.setActiveLang).toHaveBeenCalledWith('hi');
    expect(routerSpy.navigateByUrl).toHaveBeenCalledWith('/app/compatibility');
    expect(routerSpy.navigate).not.toHaveBeenCalled();
    vi.useRealTimers();
  });
});
