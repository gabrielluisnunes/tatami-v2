import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { of } from 'rxjs';
import { catchError, map } from 'rxjs/operators';
import { environment } from '../../../environments/environment';
import { AcademyService } from '../academy/academy.service';
import { hasCompletedCheckout } from '../academy/academy.models';
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
  const academyService = inject(AcademyService);
  const router = inject(Router);
  const user = authService.getUser();

  if (user?.role !== 'admin') {
    return router.createUrlTree([authService.getRoleHomeRoute()]);
  }

  if (authService.needsOnboarding()) {
    return true;
  }

  if (!environment.enforceSubscription) {
    return router.createUrlTree(['/dashboard']);
  }

  return academyService.getMyAcademy().pipe(
    map(academy =>
      hasCompletedCheckout(academy)
        ? router.createUrlTree(['/dashboard'])
        : true,
    ),
    catchError(() => of(true)),
  );
};
