import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { catchError, map, of } from 'rxjs';
import { AuthService } from './auth.service';

export const preferredLanguageGuard: CanActivateFn = (_route, state) => {
  const authService = inject(AuthService);
  const router = inject(Router);

  if (!authService.isLoggedIn()) {
    return router.createUrlTree(['/login'], {
      queryParams: {
        returnUrl: state.url
      }
    });
  }

  return authService.getMe().pipe(
    map((user) => {
      if (user.preferredLanguage) {
        return true;
      }

      return router.createUrlTree(['/select-language'], {
        queryParams: {
          returnUrl: state.url
        }
      });
    }),
    catchError(() => of(router.createUrlTree(['/login'], {
      queryParams: {
        returnUrl: state.url
      }
    })))
  );
};
