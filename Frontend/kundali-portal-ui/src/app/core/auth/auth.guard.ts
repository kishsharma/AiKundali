import { inject } from '@angular/core';
import { map } from 'rxjs';
import { CanActivateFn, Router } from '@angular/router';
import { AuthService } from './auth.service';
import { isPlusUser } from './plan.util';

export const authGuard: CanActivateFn = (_route, state) => {
  const authService = inject(AuthService);
  const router = inject(Router);

  return authService.restoreSession().pipe(map(isAuthenticated =>
    isAuthenticated
      ? true
      : router.createUrlTree(['/login'], {
          queryParams: { returnUrl: state.url }
        })
  ));
};

export const adminGuard: CanActivateFn = () => {
  const authService = inject(AuthService);
  const router = inject(Router);
  const role = authService.getCurrentUser()?.role ?? '';

  if (role.toLowerCase() === 'admin') {
    return true;
  }

  return router.createUrlTree(['/app/dashboard']);
};

export const premiumPlusGuard: CanActivateFn = (_route, state) => {
  const authService = inject(AuthService);
  const router = inject(Router);
  if (isPlusUser(authService.getCurrentUser())) return true;
  return router.createUrlTree(['/pricing'], { queryParams: { returnUrl: state.url } });
};
