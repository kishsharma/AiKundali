import { CommonModule } from '@angular/common';
import { Component, ElementRef, OnDestroy, ViewChild } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { AuthService } from '../../../core/auth/auth.service';
import { OtpChannel } from '../../../core/auth/auth.models';
import { NotificationService } from '../../../shared/services/notification.service';

type RegistrationStep = 'contact' | 'code' | 'profile';

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
  otpDigits = Array(6).fill('');
  challengeId = '';
  registrationToken = '';
  maskedDestination = '';
  verifiedChannel: OtpChannel = 'Phone';


  @ViewChild('otpInput') otpInput?: ElementRef<HTMLInputElement>;

  step: RegistrationStep = 'contact';
  otpChannel: OtpChannel = 'Phone';
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
      email: this.email.trim() || undefined,
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






