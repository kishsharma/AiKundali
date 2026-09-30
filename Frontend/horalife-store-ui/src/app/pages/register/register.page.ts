import { Component } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { readApiError } from '../../core/api/api-error';
import { AuthService } from '../../core/auth/auth.service';

@Component({
  selector: 'app-register',
  imports: [FormsModule, RouterLink],
  template: `
    <main class="auth-wrap">
      <section class="auth-card">
        <img src="assets/brand/horalife-icon.svg" alt="">
        <h1>Create account</h1>
        <p class="muted">This creates a HoraLife user. The same email can sign in to the portal.</p>

        <div class="chip-row">
          <button type="button" class="chip" [class.active]="mode === 'password'" (click)="mode = 'password'">Password</button>
          <button type="button" class="chip" [class.active]="mode === 'otp'" (click)="mode = 'otp'">Email code</button>
        </div>

        @if (mode === 'password') {
          <form (ngSubmit)="register()">
            <label class="field"><span>Full name</span><input name="fullName" [(ngModel)]="fullName" required></label>
            <label class="field"><span>Email</span><input name="email" type="email" [(ngModel)]="email" required></label>
            <label class="field"><span>Password</span><input name="password" type="password" [(ngModel)]="password" minlength="6" required></label>
            @if (errorMessage) { <div class="error">{{ errorMessage }}</div> }
            <button class="btn" type="submit" [disabled]="isLoading">{{ isLoading ? 'Creating...' : 'Create account' }}</button>
          </form>
        } @else if (step === 'contact') {
          <form (ngSubmit)="sendCode()">
            <label class="field"><span>Email</span><input name="otpEmail" type="email" [(ngModel)]="email" required></label>
            @if (errorMessage) { <div class="error">{{ errorMessage }}</div> }
            <button class="btn" type="submit" [disabled]="isLoading">Send code</button>
          </form>
        } @else if (step === 'code') {
          <form (ngSubmit)="verifyCode()">
            <p class="muted">Code sent to {{ maskedDestination }}</p>
            <label class="field"><span>Verification code</span><input name="code" [(ngModel)]="code" maxlength="6" required></label>
            @if (errorMessage) { <div class="error">{{ errorMessage }}</div> }
            <button class="btn" type="submit" [disabled]="isLoading">Verify email</button>
          </form>
        } @else {
          <form (ngSubmit)="complete()">
            <label class="field"><span>Full name</span><input name="otpName" [(ngModel)]="fullName" required></label>
            @if (errorMessage) { <div class="error">{{ errorMessage }}</div> }
            <button class="btn" type="submit" [disabled]="isLoading">Finish registration</button>
          </form>
        }

        <p>Already have an account? <a routerLink="/login">Sign in</a></p>
      </section>
    </main>
  `
})
export class RegisterPage {
  mode: 'password' | 'otp' = 'password';
  step: 'contact' | 'code' | 'profile' = 'contact';
  fullName = '';
  email = '';
  password = '';
  code = '';
  challengeId = '';
  registrationToken = '';
  maskedDestination = '';
  errorMessage = '';
  isLoading = false;

  constructor(
    private readonly auth: AuthService,
    private readonly router: Router
  ) {}

  register(): void {
    this.errorMessage = '';
    this.isLoading = true;
    this.auth.register({
      fullName: this.fullName.trim(),
      email: this.email.trim(),
      password: this.password,
      preferredLanguage: 'en'
    }).subscribe({
      next: () => void this.router.navigateByUrl('/'),
      error: (error: unknown) => {
        this.isLoading = false;
        this.errorMessage = readApiError(error, 'Registration failed.');
      }
    });
  }

  sendCode(): void {
    this.errorMessage = '';
    this.isLoading = true;
    this.auth.startOtpRegistration({ channel: 'Email', email: this.email.trim() }).subscribe({
      next: (response) => {
        this.isLoading = false;
        this.challengeId = response.challengeId;
        this.maskedDestination = response.maskedDestination;
        this.step = 'code';
      },
      error: (error: unknown) => {
        this.isLoading = false;
        this.errorMessage = readApiError(error, 'Unable to send the verification code.');
      }
    });
  }

  verifyCode(): void {
    this.errorMessage = '';
    this.isLoading = true;
    this.auth.verifyOtpRegistration({
      challengeId: this.challengeId,
      channel: 'Email',
      email: this.email.trim(),
      code: this.code.trim()
    }).subscribe({
      next: (response) => {
        this.isLoading = false;
        this.registrationToken = response.registrationToken;
        this.step = 'profile';
      },
      error: (error: unknown) => {
        this.isLoading = false;
        this.errorMessage = readApiError(error, 'Verification failed.');
      }
    });
  }

  complete(): void {
    this.errorMessage = '';
    this.isLoading = true;
    this.auth.completeOtpRegistration({
      registrationToken: this.registrationToken,
      fullName: this.fullName.trim(),
      email: this.email.trim(),
      preferredLanguage: 'en'
    }).subscribe({
      next: () => void this.router.navigateByUrl('/'),
      error: (error: unknown) => {
        this.isLoading = false;
        this.errorMessage = readApiError(error, 'Could not finish registration.');
      }
    });
  }
}
