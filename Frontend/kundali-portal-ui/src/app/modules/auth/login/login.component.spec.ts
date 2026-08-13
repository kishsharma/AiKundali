import { ComponentFixture, TestBed } from '@angular/core/testing';
import { convertToParamMap } from '@angular/router';
import { of } from 'rxjs';
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
      login: vi.fn()
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

  it('redirects to required language selection when preferredLanguage is null', () => {
    authServiceSpy.getMe.mockReturnValue(of({
      userId: 'u1',
      fullName: 'User',
      role: 'User',
      preferredLanguage: null
    }));

    (component as any).finishLogin();

    expect(routerSpy.navigate).toHaveBeenCalledWith(['/select-language'], {
      queryParams: {
        returnUrl: '/app/compatibility'
      }
    });
    expect(routerSpy.navigateByUrl).not.toHaveBeenCalled();
  });

  it('applies preferredLanguage and continues to returnUrl when language exists', () => {
    authServiceSpy.getMe.mockReturnValue(of({
      userId: 'u1',
      fullName: 'User',
      role: 'User',
      preferredLanguage: 'hi'
    }));

    (component as any).finishLogin();

    expect(translocoServiceSpy.setActiveLang).toHaveBeenCalledWith('hi');
    expect(routerSpy.navigateByUrl).toHaveBeenCalledWith('/app/compatibility');
    expect(routerSpy.navigate).not.toHaveBeenCalled();
  });
});
