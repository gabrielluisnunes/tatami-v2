import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { of } from 'rxjs';
import { catchError, map } from 'rxjs/operators';
import { environment } from '../../../environments/environment';
import { Academy } from '../academy/academy.models';
import { AcademyService } from '../academy/academy.service';

const blockedStatuses = new Set([
  'past_due',
  'unpaid',
  'canceled',
  'incomplete',
  'incomplete_expired',
]);

export const subscriptionRequiredGuard: CanActivateFn = (_route, state) => {
  const academyService = inject(AcademyService);
  const router = inject(Router);

  if (!environment.enforceSubscription) {
    return true;
  }

  if (
    state.url.startsWith('/dashboard/assinatura') ||
    state.url.startsWith('/onboarding')
  ) {
    return true;
  }

  return academyService.getMyAcademy().pipe(
    map(academy =>
      shouldLockDashboard(academy)
        ? router.createUrlTree(['/dashboard/assinatura'])
        : true,
    ),
    catchError(() => of(true)),
  );
};

function shouldLockDashboard(academy: Academy): boolean {
  const status = academy.subscriptionStatus;
  const isTrialing = status === 'trial' || status === 'trialing';
  const hasNeverCompletedCheckout = !academy.plan || !academy.stripeCustomerId;

  return blockedStatuses.has(status) || (isTrialing && hasNeverCompletedCheckout);
}
