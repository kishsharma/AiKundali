import { CommonModule } from '@angular/common';
import { ChangeDetectorRef, Component, ElementRef, OnDestroy, ViewChild } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { TranslocoService } from '@jsverse/transloco';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { AuthService } from '../../../core/auth/auth.service';
import { GoogleSignInButtonComponent } from '../google-sign-in/google-sign-in-button.component';
import { OtpChannel, PreferredLanguage } from '../../../core/auth/auth.models';
import { NotificationService } from '../../../shared/services/notification.service';
import { isAllowedRegistrationEmail } from '../../../shared/utils/common.utils';

type RegistrationMode = 'otp' | 'password';
type RegistrationStep = 'contact' | 'code' | 'profile';

@Component({
  selector: 'app-register',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink, GoogleSignInButtonComponent],
  templateUrl: './register.component.html',
  styleUrl: '../login/login.component.scss'
})
export class RegisterComponent implements OnDestroy {
  fullName = '';
  email = '';
  phoneNumber = '';
  password = '';
  otpCode = '';
  otpDigits = Array(6).fill('');
  challengeId = '';
  registrationToken = '';
  maskedDestination = '';
  verifiedChannel: OtpChannel = 'Email';
  preferredLanguage: PreferredLanguage = 'en';


  @ViewChild('otpInput') otpInput?: ElementRef<HTMLInputElement>;

  registrationMode: RegistrationMode = 'otp';
  step: RegistrationStep = 'contact';
  otpChannel: OtpChannel = 'Email';
  resendSeconds = 0;
  isLoading = false;
  isExiting = false;
  errorMessage = '';

  private resendTimer?: ReturnType<typeof setInterval>;
  private navigationTimer?: ReturnType<typeof setTimeout>;

  constructor(
    private authService: AuthService,
    private router: Router,
    private route: ActivatedRoute,
    private translocoService: TranslocoService,
    private notifications: NotificationService,
    private cdr: ChangeDetectorRef
  ) {}

  ngOnDestroy(): void {
    this.stopResendTimer();
    if (this.navigationTimer) {
      clearTimeout(this.navigationTimer);
    }
  }

  get otpContactLabel(): string {
    return this.otpChannel === 'Phone' ? 'Mobile number' : 'Email';
  }

  get otpContactPlaceholder(): string {
    return this.otpChannel === 'Phone' ? 'Enter your mobile number' : 'Enter your email';
  }

  get verifiedDestination(): string {
    return this.verifiedChannel === 'Phone' ? this.phoneNumber : this.email;
  }

  useOtpChannel(channel: OtpChannel): void {
    if (this.otpChannel === channel) {
      return;
    }

    this.otpChannel = channel;
    this.errorMessage = '';
    this.step = 'contact';
    this.resetOtpDigits();
    this.challengeId = '';
    this.registrationToken = '';
    this.maskedDestination = '';
    this.stopResendTimer();
  }

  sendRegistrationCode(): void {
    this.errorMessage = '';
    const request = this.buildOtpRequest();
    if (!request) {
      return;
    }

    this.isLoading = true;
    this.authService.startOtpRegistration(request).subscribe({
      next: (response) => {
        this.isLoading = false;
        this.challengeId = response.challengeId;
        this.maskedDestination = response.maskedDestination;
        this.step = 'code';
        this.resetOtpDigits();
        this.cdr.detectChanges();
        this.startResendTimer(response.resendAfterSeconds || 60);
        setTimeout(() => this.focusOtpInput());
        this.notifications.success(response.message || 'Verification code sent.');
      },
      error: (error) => {
        this.isLoading = false;
        this.notifications.error(error?.error?.message || 'Unable to send verification code.');
      }
    });
  }

  verifyRegistrationCode(): void {
    this.errorMessage = '';
    const request = this.buildOtpRequest();
    if (!request) {
      return;
    }

    if (!this.challengeId || this.otpDigits.some((digit) => !digit)) {
      this.errorMessage = 'Please enter the full 6 digit verification code.';
      return;
    }

    this.isLoading = true;
    this.authService.verifyOtpRegistration({
      ...request,
      challengeId: this.challengeId,
      code: this.otpCode.trim()
    }).subscribe({
      next: (response) => {
        this.isLoading = false;
        this.registrationToken = response.registrationToken;
        this.verifiedChannel = this.otpChannel;
        this.step = 'profile';
        this.stopResendTimer();
        this.cdr.detectChanges();
      },
      error: (error) => {
        this.isLoading = false;
        this.notifications.error(error?.error?.message || 'Verification failed. Please try again.');
      }
    });
  }

  completeRegistration(): void {
    this.errorMessage = '';
    if (!this.fullName.trim()) {
      this.errorMessage = 'Please enter your full name.';
      return;
    }

    const optionalEmail = this.email.trim();
    if (optionalEmail && !isAllowedRegistrationEmail(optionalEmail)) {
      this.errorMessage = 'Enter a valid email address.';
      return;
    }

    this.isLoading = true;
    this.authService.completeOtpRegistration({
      registrationToken: this.registrationToken,
      fullName: this.fullName.trim(),
      email: this.email.trim() || undefined,
      phoneNumber: this.phoneNumber.trim() || undefined,
      preferredLanguage: this.preferredLanguage
    }).subscribe({
      next: () => this.finishRegistration(),
      error: (error) => {
        this.isLoading = false;
        this.notifications.error(error?.error?.message || 'Registration failed. Please try again.');
      }
    });
  }

  loginWithGoogle(idToken: string): void {
    if (this.isLoading) {
      return;
    }

    this.errorMessage = '';
    this.isLoading = true;
    this.authService.loginWithGoogle(idToken).subscribe({
      next: () => this.finishGoogleSignIn(),
      error: (error) => {
        this.isLoading = false;
        this.notifications.error(this.googleSignInError(error));
      }
    });
  }

  onGoogleSignInFailed(message: string): void {
    this.notifications.error(message);
  }

  private googleSignInError(error: { error?: { message?: string; title?: string } | string }): string {
    const body = error?.error;
    if (typeof body === 'string' && body.trim()) {
      return body;
    }

    if (body && typeof body === 'object') {
      return body.message || body.title || 'Google sign-in failed. Please try again.';
    }

    return 'Google sign-in failed. Please try again.';
  }

  registerWithPassword(): void {
    this.errorMessage = '';
    if (!this.fullName.trim() || !this.email.trim() || !this.password) {
      this.errorMessage = 'Please enter full name, email and password.';
      return;
    }

    if (!isAllowedRegistrationEmail(this.email)) {
      this.errorMessage = 'Enter a valid email address.';
      return;
    }

    this.isLoading = true;
    this.authService.register({
      fullName: this.fullName.trim(),
      email: this.email.trim(),
      phoneNumber: this.phoneNumber.trim() || undefined,
      password: this.password,
      preferredLanguage: this.preferredLanguage
    }).subscribe({
      next: () => this.finishRegistration(),
      error: (error) => {
        this.isLoading = false;
        this.notifications.error(error?.error?.message || 'Registration failed. Please try again.');
      }
    });
  }

  usePasswordRegistration(): void {
    this.errorMessage = '';
    this.registrationMode = 'password';
    this.step = 'contact';
    this.stopResendTimer();
  }

  useOtpRegistration(): void {
    this.errorMessage = '';
    this.registrationMode = 'otp';
    this.step = 'contact';
  }

  editContact(): void {
    this.step = 'contact';
    this.resetOtpDigits();
    this.challengeId = '';
    this.registrationToken = '';
    this.stopResendTimer();
  }

  onOtpInput(value: string): void {
    this.otpCode = value.replace(/\D/g, '').slice(0, 6);
    this.syncOtpDigits();
  }

  onOtpKeyDown(event: KeyboardEvent): void {
    if (event.key === 'Enter' && this.otpCode.length === 6) {
      this.verifyRegistrationCode();
    }
  }
  private buildOtpRequest(): { channel: OtpChannel; email?: string; phoneNumber?: string } | null {
    if (this.otpChannel === 'Phone') {
      const phoneNumber = this.phoneNumber.trim();
      if (!phoneNumber) {
        this.errorMessage = 'Please enter your mobile number.';
        return null;
      }

      return { channel: 'Phone', phoneNumber };
    }

    const email = this.email.trim();
    if (!email) {
      this.errorMessage = 'Please enter your email.';
      return null;
    }

    if (!isAllowedRegistrationEmail(email)) {
      this.errorMessage = 'Enter a valid email address.';
      return null;
    }

    return { channel: 'Email', email };
  }


  private syncOtpDigits(): void {
    this.otpDigits = Array.from({ length: 6 }, (_, index) => this.otpCode[index] || '');
  }

  private resetOtpDigits(): void {
    this.otpCode = '';
    this.syncOtpDigits();
  }

  focusOtpInput(): void {
    this.otpInput?.nativeElement.focus();
  }
  private finishGoogleSignIn(): void {
    this.authService.getMe().subscribe({
      next: (user) => {
        const returnUrl = this.route.snapshot.queryParamMap.get('returnUrl') || '/app/dashboard';
        this.isExiting = true;
        this.cdr.detectChanges();

        this.navigationTimer = setTimeout(() => {
          this.isLoading = false;
          if (user.preferredLanguage) {
            this.translocoService.setActiveLang(user.preferredLanguage as PreferredLanguage);
            this.router.navigateByUrl(returnUrl);
            return;
          }

          this.router.navigate(['/select-language'], {
            queryParams: {
              returnUrl
            }
          });
        }, 260);
      },
      error: () => {
        this.isLoading = false;
        this.isExiting = false;
        this.notifications.error('Sign-in successful, but user profile failed.');
        this.cdr.detectChanges();
      }
    });
  }

  private finishRegistration(): void {
    this.authService.getMe().subscribe({
      next: (user) => {
        const language = (user.preferredLanguage || this.preferredLanguage) as PreferredLanguage;
        this.translocoService.setActiveLang(language);
        this.isExiting = true;
        this.cdr.detectChanges();

        this.navigationTimer = setTimeout(() => {
          this.isLoading = false;
          this.router.navigate(['/app/dashboard']);
        }, 260);
      },
      error: () => {
        this.isLoading = false;
        this.isExiting = false;
        this.notifications.error('Registration successful, but user profile failed. Please login again.');
        this.cdr.detectChanges();
      }
    });
  }
  private startResendTimer(seconds: number): void {
    this.stopResendTimer();
    this.resendSeconds = seconds;
    this.resendTimer = setInterval(() => {
      this.resendSeconds = Math.max(0, this.resendSeconds - 1);
      if (this.resendSeconds === 0) {
        this.stopResendTimer(false);
      }
    }, 1000);
  }

  private stopResendTimer(reset = true): void {
    if (this.resendTimer) {
      clearInterval(this.resendTimer);
      this.resendTimer = undefined;
    }

    if (reset) {
      this.resendSeconds = 0;
    }
  }
}













