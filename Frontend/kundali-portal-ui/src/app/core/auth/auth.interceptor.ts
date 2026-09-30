import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { catchError, throwError } from 'rxjs';
import { AuthService } from './auth.service';

export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const authService = inject(AuthService);
  const router = inject(Router);
  const token = authService.getToken();
  const authRequest = token
    ? req.clone({
        setHeaders: {
          Authorization: `Bearer ${token}`
        }
      })
    : req;

  return next(authRequest).pipe(
    catchError((error: unknown) => {
      if (error instanceof HttpErrorResponse && shouldHandleSessionExpiry(error, req.url)) {
        authService.clearSession();
        router.navigate(['/login'], {
          queryParams: {
            sessionExpired: '1',
            returnUrl: router.url.startsWith('/login') ? '/app/dashboard' : router.url
          }
        });
      }

      return throwError(() => error);
    })
  );
};

function shouldHandleSessionExpiry(error: HttpErrorResponse, requestUrl: string): boolean {
  if (error.status !== 401) {
    return false;
  }

  return !/\/auth\/(login|register|otp\/start-login|otp\/verify-login|otp\/start-registration|otp\/verify-registration|otp\/complete-registration)$/i.test(requestUrl);
}
