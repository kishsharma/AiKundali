import { ComponentFixture, TestBed } from '@angular/core/testing';
import { of } from 'rxjs';
import { TranslocoService } from '@jsverse/transloco';
import { Router } from '@angular/router';
import { AuthService } from '../../../core/auth/auth.service';
import { NotificationService } from '../../../shared/services/notification.service';
import { RegisterComponent } from './register.component';

describe('RegisterComponent', () => {
  let component: RegisterComponent;
  let fixture: ComponentFixture<RegisterComponent>;
  let authServiceSpy: any;
  let routerSpy: any;
  let notificationsSpy: any;
  let translocoServiceSpy: any;

  beforeEach(async () => {
    authServiceSpy = {
      startOtpRegistration: vi.fn(),
      verifyOtpRegistration: vi.fn(),
      completeOtpRegistration: vi.fn(),
      register: vi.fn(),
      getMe: vi.fn()
    };
    routerSpy = { navigate: vi.fn() };
    notificationsSpy = {
      success: vi.fn(),
      error: vi.fn(),
      warning: vi.fn()
    };
    translocoServiceSpy = { setActiveLang: vi.fn() };

    await TestBed.configureTestingModule({
      imports: [RegisterComponent],
      providers: [
        { provide: AuthService, useValue: authServiceSpy },
        { provide: Router, useValue: routerSpy },
        { provide: NotificationService, useValue: notificationsSpy },
        { provide: TranslocoService, useValue: translocoServiceSpy }
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(RegisterComponent);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('sends preferredLanguage in complete OTP registration and applies it', () => {
    vi.useFakeTimers();
    authServiceSpy.completeOtpRegistration.mockReturnValue(of({
      userId: 'u1',
      fullName: 'User',
      role: 'User',
      token: 'token',
      preferredLanguage: 'kn'
    }));
    authServiceSpy.getMe.mockReturnValue(of({ preferredLanguage: 'kn' }));

    component.registrationToken = 'reg-token';
    component.fullName = 'Test User';
    component.email = 'test@example.com';
    component.phoneNumber = '9999999999';
    component.preferredLanguage = 'kn';

    component.completeRegistration();

    expect(authServiceSpy.completeOtpRegistration).toHaveBeenCalledWith({
      registrationToken: 'reg-token',
      fullName: 'Test User',
      email: 'test@example.com',
      phoneNumber: '9999999999',
      preferredLanguage: 'kn'
    });
    expect(authServiceSpy.getMe).toHaveBeenCalled();
    expect(translocoServiceSpy.setActiveLang).toHaveBeenCalledWith('kn');
    expect(component.isExiting).toBe(true);
    expect(routerSpy.navigate).not.toHaveBeenCalled();

    vi.advanceTimersByTime(260);

    expect(routerSpy.navigate).toHaveBeenCalledWith(['/app/dashboard']);
    vi.useRealTimers();
  });

  it('sends preferredLanguage in password registration and applies it', () => {
    vi.useFakeTimers();
    authServiceSpy.register.mockReturnValue(of({
      userId: 'u1',
      fullName: 'User',
      role: 'User',
      token: 'token',
      preferredLanguage: 'hi'
    }));
    authServiceSpy.getMe.mockReturnValue(of({ preferredLanguage: 'hi' }));

    component.fullName = 'Test User';
    component.email = 'test@example.com';
    component.phoneNumber = '';
    component.password = 'Passw0rd!';
    component.preferredLanguage = 'hi';

    component.registerWithPassword();

    expect(authServiceSpy.register).toHaveBeenCalledWith({
      fullName: 'Test User',
      email: 'test@example.com',
      phoneNumber: undefined,
      password: 'Passw0rd!',
      preferredLanguage: 'hi'
    });
    expect(authServiceSpy.getMe).toHaveBeenCalled();
    expect(translocoServiceSpy.setActiveLang).toHaveBeenCalledWith('hi');
    expect(component.isExiting).toBe(true);
    expect(routerSpy.navigate).not.toHaveBeenCalled();

    vi.advanceTimersByTime(260);

    expect(routerSpy.navigate).toHaveBeenCalledWith(['/app/dashboard']);
    vi.useRealTimers();
  });
});

