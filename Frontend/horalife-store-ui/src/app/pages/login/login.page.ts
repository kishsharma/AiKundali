import { Component, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { readApiError } from '../../core/api/api-error';
import { AuthService } from '../../core/auth/auth.service';

@Component({
  selector: 'app-login',
  imports: [FormsModule, RouterLink],
  template: `
    <main class="auth-wrap">
      <section class="auth-card">
        <img src="assets/brand/horalife-icon.svg" alt="">
        <h1>Sign in</h1>
        <p class="muted">Use the same HoraLife email and password as the portal. This store keeps its own session.</p>

        @if (sessionExpired) {
          <div class="warning">Your store session expired. Sign in again.</div>
        }

        <div class="chip-row">
          <button type="button" class="chip" [class.active]="mode === 'password'" (click)="mode = 'password'">Password</button>
          <button type="button" class="chip" [class.active]="mode === 'otp'" (click)="mode = 'otp'">Email code</button>
        </div>

        @if (mode === 'password') {
          <form (ngSubmit)="login()">
            <label class="field"><span>Email</span><input name="email" type="email" [(ngModel)]="email" autocomplete="username" required></label>
            <label class="field"><span>Password</span><input name="password" type="password" [(ngModel)]="password" autocomplete="current-password" required></label>
            @if (errorMessage) { <div class="error">{{ errorMessage }}</div> }
            <button class="btn" type="submit" [disabled]="isLoading">{{ isLoading ? 'Signing in...' : 'Sign in' }}</button>
          </form>
        } @else if (otpStep === 'contact') {
          <form (ngSubmit)="sendCode()">
            <label class="field"><span>Email</span><input name="otpEmail" type="email" [(ngModel)]="email" autocomplete="username" required></label>
            @if (errorMessage) { <div class="error">{{ errorMessage }}</div> }
            <button class="btn" type="submit" [disabled]="isLoading">{{ isLoading ? 'Sending...' : 'Send code' }}</button>
          </form>
        } @else {
          <form (ngSubmit)="verifyCode()">
            <p class="muted">Code sent to {{ maskedDestination }}</p>
            <label class="field"><span>Verification code</span><input name="code" [(ngModel)]="code" inputmode="numeric" maxlength="6" required></label>
            @if (errorMessage) { <div class="error">{{ errorMessage }}</div> }
            <div class="actions">
              <button class="btn" type="submit" [disabled]="isLoading">Verify</button>
              <button class="btn-secondary" type="button" (click)="otpStep = 'contact'">Change email</button>
            </div>
          </form>
        }

        <p>New to HoraLife? <a routerLink="/register">Create an account</a></p>
        <p><a routerLink="/">Browse the catalogue</a></p>
      </section>
    </main>
  `
})
export class LoginPage implements OnInit {
  mode: 'password' | 'otp' = 'password';
  otpStep: 'contact' | 'code' = 'contact';
  email = '';
  password = '';
  code = '';
  challengeId = '';
  maskedDestination = '';
  errorMessage = '';
  isLoading = false;
  sessionExpired = false;
  private returnUrl = '/';

  constructor(
    private readonly auth: AuthService,
    private readonly router: Router,
    private readonly route: ActivatedRoute
  ) {}

  ngOnInit(): void {
    this.sessionExpired = this.route.snapshot.queryParamMap.get('sessionExpired') === '1';
    this.returnUrl = this.route.snapshot.queryParamMap.get('returnUrl') || '/';
    if (this.auth.isLoggedIn()) {
      void this.router.navigateByUrl(this.returnUrl);
    }
  }

  login(): void {
    this.errorMessage = '';
    this.isLoading = true;
    this.auth.login({ email: this.email.trim(), password: this.password }).subscribe({
      next: () => void this.router.navigateByUrl(this.returnUrl),
      error: (error: unknown) => {
        this.isLoading = false;
        this.errorMessage = readApiError(error, 'Login failed. Check the email and password.');
      }
    });
  }

  sendCode(): void {
    this.errorMessage = '';
    this.isLoading = true;
    this.auth.startOtpLogin({ channel: 'Email', email: this.email.trim() }).subscribe({
      next: (response) => {
        this.isLoading = false;
        this.challengeId = response.challengeId;
        this.maskedDestination = response.maskedDestination;
        this.otpStep = 'code';
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
    this.auth.verifyOtpLogin({
      challengeId: this.challengeId,
      channel: 'Email',
      email: this.email.trim(),
      code: this.code.trim()
    }).subscribe({
      next: () => void this.router.navigateByUrl(this.returnUrl),
      error: (error: unknown) => {
        this.isLoading = false;
        this.errorMessage = readApiError(error, 'Verification failed.');
      }
    });
  }
}
