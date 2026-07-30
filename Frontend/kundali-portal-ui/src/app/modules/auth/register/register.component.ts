import { CommonModule } from '@angular/common';
import { Component, OnDestroy } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { AuthService } from '../../../core/auth/auth.service';
import { NotificationService } from '../../../shared/services/notification.service';

type RegistrationStep = 'email' | 'code' | 'profile';

@Component({
  selector: 'app-register',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  templateUrl: './register.component.html',
  styleUrl: '../login/login.component.scss'
})
export class RegisterComponent implements OnDestroy {
  fullName = '';
  email = '';
  phoneNumber = '';
  otpCode = '';
  challengeId = '';
  registrationToken = '';
  maskedDestination = '';

  step: RegistrationStep = 'email';
  resendSeconds = 0;
  isLoading = false;
  errorMessage = '';

  private resendTimer?: ReturnType<typeof setInterval>;

  constructor(
    private authService: AuthService,
    private router: Router,
    private notifications: NotificationService
  ) {}

  ngOnDestroy(): void {
    this.stopResendTimer();
  }

  sendRegistrationCode(): void {
    this.errorMessage = '';
    const email = this.email.trim();
    if (!email) {
      this.errorMessage = 'Please enter your email.';
      return;
    }

    this.isLoading = true;
    this.authService.startOtpRegistration({ email }).subscribe({
      next: (response) => {
        this.isLoading = false;
        this.challengeId = response.challengeId;
        this.maskedDestination = response.maskedDestination;
        this.step = 'code';
        this.startResendTimer(response.resendAfterSeconds || 60);
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
    if (!this.challengeId || !this.otpCode.trim()) {
      this.errorMessage = 'Please enter the verification code.';
      return;
    }

    this.isLoading = true;
    this.authService.verifyOtpRegistration({
      challengeId: this.challengeId,
      email: this.email.trim(),
      code: this.otpCode.trim()
    }).subscribe({
      next: (response) => {
        this.isLoading = false;
        this.registrationToken = response.registrationToken;
        this.step = 'profile';
        this.stopResendTimer();
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

    this.isLoading = true;
    this.authService.completeOtpRegistration({
      registrationToken: this.registrationToken,
      fullName: this.fullName.trim(),
      phoneNumber: this.phoneNumber.trim() || undefined
    }).subscribe({
      next: () => {
        this.isLoading = false;
        this.router.navigate(['/app/dashboard']);
      },
      error: (error) => {
        this.isLoading = false;
        this.notifications.error(error?.error?.message || 'Registration failed. Please try again.');
      }
    });
  }

  editEmail(): void {
    this.step = 'email';
    this.otpCode = '';
    this.challengeId = '';
    this.registrationToken = '';
    this.stopResendTimer();
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
