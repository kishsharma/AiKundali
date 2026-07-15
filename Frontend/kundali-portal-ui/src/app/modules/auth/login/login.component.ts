import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { AuthService } from '../../../core/auth/auth.service';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  templateUrl: './login.component.html',
  styleUrl: './login.component.scss'
})
export class LoginComponent {
  email = '';
  password = '';

  isLoading = false;
  errorMessage = '';

  constructor(
    private authService: AuthService,
    private router: Router
  ) {}

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
      next: (loginresponse) => {
        console.log('Login success:', loginresponse);
         // Call protected API after login
      this.authService.getMe().subscribe({
        next: (user) => {
          console.log('Protected API user:', user);
          this.isLoading = false;
          this.router.navigate(['/app/dashboard']);
        },
        error: (err) => {
          console.error('Protected API failed:', err);

          this.isLoading = false;
          this.errorMessage = 'Login successful, but user profile failed.';
        }
      });
    },
        error: (error) => {
        this.isLoading = false;
        this.errorMessage = error?.error?.message || 'Login failed. Please try again.';
      }
    });
  }

}
