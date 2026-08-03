import { CommonModule } from '@angular/common';
import { Component, ElementRef, OnDestroy, OnInit, ViewChild } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { AuthService } from '../../../core/auth/auth.service';
import { OtpChannel } from '../../../core/auth/auth.models';
import { NotificationService } from '../../../shared/services/notification.service';

type LoginMode = 'otp' | 'password';
type OtpStep = 'contact' | 'code';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  templateUrl: './login.component.html',
  styleUrl: './login.component.scss'
})
export class LoginComponent implements OnInit, OnDestroy {
  email = '';
  phoneNumber = '';
  password = '';
  otpCode = '';
  otpDigits = Array(6).fill('');
  challengeId = '';
  maskedDestination = '';


  @ViewChild('otpInput') otpInput?: ElementRef<HTMLInputElement>;

  loginMode: LoginMode = 'otp';
  otpStep: OtpStep = 'contact';
  otpChannel: OtpChannel = 'Phone';
  resendSeconds = 0;

  isLoading = false;
  errorMessage = '';

  private resendTimer?: ReturnType<typeof setInterval>;

  constructor(
    private authService: AuthService,
    private router: Router,
    private route: ActivatedRoute,
    private notifications: NotificationService
  ) {}

  ngOnInit(): void {
    if (this.route.snapshot.queryParamMap.get('sessionExpired') === '1') {
      this.notifications.warning('Your session has expired. Please sign in again.');
    }
  }

  ngOnDestroy(): void {
    this.stopResendTimer();
  }

  get otpContactLabel(): string {
    return this.otpChannel === 'Phone' ? 'Mobile number' : 'Email';
  }

  get otpContactPlaceholder(): string {
    return this.otpChannel === 'Phone' ? 'Enter your mobile number' : 'Enter your email';
  }

  get otpContactAutocomplete(): string {
    return this.otpChannel === 'Phone' ? 'tel' : 'email';
  }

  get otpContactInputType(): string {
    return this.otpChannel === 'Phone' ? 'tel' : 'email';
  }

  useOtpChannel(channel: OtpChannel): void {
    if (this.otpChannel === channel) {
      return;
    }

    this.otpChannel = channel;
    this.errorMessage = '';
    this.otpStep = 'contact';
    this.challengeId = '';
    this.maskedDestination = '';
    this.resetOtpDigits();
    this.stopResendTimer();
  }

  sendLoginCode(): void {
    this.errorMessage = '';
    const request = this.buildOtpRequest();
    if (!request) {
      return;
    }

    this.isLoading = true;
    this.authService.startOtpLogin(request).subscribe({
      next: (response) => {
        this.isLoading = false;
        this.challengeId = response.challengeId;
        this.maskedDestination = response.maskedDestination;
        this.otpStep = 'code';
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

  verifyLoginCode(): void {
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
    this.authService.verifyOtpLogin({
      ...request,
      challengeId: this.challengeId,
      code: this.otpCode.trim()
    }).subscribe({
      next: () => this.finishLogin(),
      error: (error) => {
        this.isLoading = false;
        this.notifications.error(error?.error?.message || 'Verification failed. Please try again.');
      }
    });
  }

  login(): void {
    this.errorMessage = '';

    if (!this.email || !this.password) {
      this.errorMessage = 'Please enter email and password.';
      return;
    }

    this.isLoading = true;

    this.authService.login({
      email: this.email,
      password: this.password
    }).subscribe({
      next: () => this.finishLogin(),
      error: (error) => {
        this.isLoading = false;
        this.notifications.error(error?.error?.message || 'Login failed. Please try again.');
      }
    });
  }

  usePasswordLogin(): void {
    this.errorMessage = '';
    this.loginMode = 'password';
  }

  useOtpLogin(): void {
    this.errorMessage = '';
    this.loginMode = 'otp';
  }

  editContact(): void {
    this.otpStep = 'contact';
    this.resetOtpDigits();
    this.challengeId = '';
    this.stopResendTimer();
  }

  onOtpInput(value: string): void {
    this.otpCode = value.replace(/\D/g, '').slice(0, 6);
    this.syncOtpDigits();
  }

  onOtpKeyDown(event: KeyboardEvent): void {
    if (event.key === 'Enter' && this.otpCode.length === 6) {
      this.verifyLoginCode();
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
  private finishLogin(): void {
    this.authService.getMe().subscribe({
      next: () => {
        this.isLoading = false;
        this.router.navigateByUrl(this.route.snapshot.queryParamMap.get('returnUrl') || '/app/dashboard');
      },
      error: () => {
        this.isLoading = false;
        this.notifications.error('Login successful, but user profile failed.');
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







