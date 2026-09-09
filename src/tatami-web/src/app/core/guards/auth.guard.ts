import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthService } from '../auth/auth.service';
import {
  IdleSessionService,
  SESSION_EXPIRED_REASON,
} from '../auth/idle-session.service';

export const authGuard: CanActivateFn = () => {
  const authService = inject(AuthService);
  const idleSession = inject(IdleSessionService);
  const router = inject(Router);

  if (authService.isAuthenticated() && !idleSession.hasIdleExpired()) {
    return true;
  }

  const expired =
    idleSession.hasIdleExpired() ||
    authService.isAccessTokenExpired() ||
    !!authService.getAccessToken();

  idleSession.stop();
  authService.clearSession();

  return router.createUrlTree(
    ['/login'],
    expired ? { queryParams: { reason: SESSION_EXPIRED_REASON } } : {},
  );
};
