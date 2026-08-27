import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthStore } from './auth-store';

export const moderatorGuard: CanActivateFn = () => {
  const router = inject(Router);

  return inject(AuthStore).isModerator() || router.createUrlTree(['/']);
};
