import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';

import { AuthService } from '../services/auth.service';

/**
 * Frontend route guards keep the UI tidy for each role. They are a usability
 * layer only: every API endpoint validates the caller's role server-side.
 */
export const authGuard: CanActivateFn = () => {
  const auth = inject(AuthService);
  const router = inject(Router);

  if (auth.getToken()) {
    return true;
  }

  return router.createUrlTree(['/login']);
};

export function roleGuard(...allowedRoles: string[]): CanActivateFn {
  return () => {
    const auth = inject(AuthService);
    const router = inject(Router);

    if (!auth.getToken()) {
      return router.createUrlTree(['/login']);
    }

    const role = auth.getRole();

    if (allowedRoles.some(allowed => allowed === role)) {
      return true;
    }

    // Signed in with the wrong role: return to the dashboard the user can use.
    return router.createUrlTree(['/dashboard']);
  };
}
