import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthService } from '../services/auth.service';

/**
 * Blocks routes that require a signed-in learner (e.g. saved progress).
 * Unauthenticated visitors are redirected into the onboarding/welcome flow,
 * preserving where they were headed so we can return them after signup.
 */
export const authGuard: CanActivateFn = (_route, state) => {
  const authService = inject(AuthService);
  const router = inject(Router);

  if (authService.currentUser()) {
    return true;
  }

  return router.createUrlTree(['/welcome'], {
    queryParams: { returnUrl: state.url },
  });
};
