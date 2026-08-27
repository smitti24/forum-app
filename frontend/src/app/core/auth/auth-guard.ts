import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthStore } from './auth-store';

export const authGuard: CanActivateFn = (_route, state) => {
  const router = inject(Router);

  return (
    inject(AuthStore).isAuthenticated() ||
    router.createUrlTree(['/login'], { queryParams: { returnUrl: state.url } })
  );
};
