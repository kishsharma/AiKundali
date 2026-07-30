import { CommonModule } from '@angular/common';
import { Component, OnDestroy, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { AuthService } from '../../../core/auth/auth.service';
import { NotificationService } from '../../../shared/services/notification.service';

type LoginMode = 'otp' | 'password';
type OtpStep = 'email' | 'code';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  templateUrl: './login.component.html',
  styleUrl: './login.component.scss'
})
export class LoginComponent implements OnInit, OnDestroy {
  email = '';
  password = '';
  otpCode = '';
  challengeId = '';
  maskedDestination = '';

  loginMode: LoginMode = 'otp';
  otpStep: OtpStep = 'email';
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

  sendLoginCode(): void {
    this.errorMessage = '';
    const email = this.email.trim();
    if (!email) {
      this.errorMessage = 'Please enter your email.';
      return;
    }

    this.isLoading = true;
    this.authService.startOtpLogin({ email }).subscribe({
      next: (response) => {
        this.isLoading = false;
        this.challengeId = response.challengeId;
        this.maskedDestination = response.maskedDestination;
        this.otpStep = 'code';
        this.startResendTimer(response.resendAfterSeconds || 60);
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
    if (!this.challengeId || !this.otpCode.trim()) {
      this.errorMessage = 'Please enter the verification code.';
      return;
    }

    this.isLoading = true;
    this.authService.verifyOtpLogin({
      challengeId: this.challengeId,
      email: this.email.trim(),
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

  editEmail(): void {
    this.otpStep = 'email';
    this.otpCode = '';
    this.challengeId = '';
    this.stopResendTimer();
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
