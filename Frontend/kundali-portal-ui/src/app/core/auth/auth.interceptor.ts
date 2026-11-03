import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { catchError, switchMap, throwError } from 'rxjs';
import { AuthService } from './auth.service';

export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const authService = inject(AuthService);
  const router = inject(Router);
  const token = authService.getToken();
  const authRequest = token && shouldAttachAccessToken(req.url)
    ? req.clone({
        setHeaders: {
          Authorization: `Bearer ${token}`
        }
      })
    : req;

  return next(authRequest).pipe(
    catchError((error: unknown) => {
      if (error instanceof HttpErrorResponse && shouldHandleSessionExpiry(error, req.url)) {
        if (token) {
          return authService.refreshSession().pipe(
            switchMap(response => next(req.clone({
              setHeaders: { Authorization: `Bearer ${response.token}` }
            }))),
            catchError(refreshError => {
              expireSession(authService, router);
              return throwError(() => refreshError);
            })
          );
        }

        expireSession(authService, router);
      }

      return throwError(() => error);
    })
  );
};

function shouldAttachAccessToken(requestUrl: string): boolean {
  return !/\/auth\/(login|register|google|refresh|logout|otp\/start-login|otp\/verify-login|otp\/start-registration|otp\/verify-registration|otp\/complete-registration|store-handoff\/redeem)$/i.test(requestUrl);
}

function expireSession(authService: AuthService, router: Router): void {
  authService.clearSession();
  router.navigate(['/login'], {
    queryParams: {
      sessionExpired: '1',
      returnUrl: router.url.startsWith('/login') ? '/app/dashboard' : router.url
    }
  });
}

function shouldHandleSessionExpiry(error: HttpErrorResponse, requestUrl: string): boolean {
  if (error.status !== 401) {
    return false;
  }

  if (/\/auth\/(google|refresh|logout|store-handoff\/redeem)$/i.test(requestUrl)) {
    return false;
  }

  return !/\/auth\/(login|register|otp\/start-login|otp\/verify-login|otp\/start-registration|otp\/verify-registration|otp\/complete-registration)$/i.test(requestUrl);
}
