import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthService } from '../auth/auth.service';

export const onboardingRequiredGuard: CanActivateFn = () => {
  const authService = inject(AuthService);
  const router = inject(Router);
  const user = authService.getUser();

  if (user?.role === 'professor') {
    return router.createUrlTree(['/professor']);
  }

  if (user?.role === 'aluno') {
    return router.createUrlTree(['/aluno']);
  }

  if (authService.needsOnboarding()) {
    return router.createUrlTree(['/onboarding']);
  }

  return true;
};

export const onboardingCompleteGuard: CanActivateFn = () => {
  const authService = inject(AuthService);
  const router = inject(Router);
  const user = authService.getUser();

  if (user?.role !== 'admin') {
    return router.createUrlTree([authService.getRoleHomeRoute()]);
  }

  if (!authService.needsOnboarding()) {
    return router.createUrlTree(['/dashboard']);
  }

  return true;
};
