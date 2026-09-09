import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { catchError, throwError } from 'rxjs';
import { AuthService } from './auth.service';
import { SESSION_EXPIRED_REASON } from './idle-session.service';

export const authInterceptor: HttpInterceptorFn = (request, next) => {
  const authService = inject(AuthService);
  const router = inject(Router);
  const token = authService.getAccessToken();

  const authedRequest = token
    ? request.clone({
        setHeaders: {
          Authorization: `Bearer ${token}`,
        },
      })
    : request;

  return next(authedRequest).pipe(
    catchError((error: HttpErrorResponse) => {
      const isCredentialRequest =
        request.url.includes('/api/auth/login') ||
        request.url.includes('/api/auth/register');

      if (
        error.status === 401 &&
        !isCredentialRequest &&
        authService.isAccessTokenExpired()
      ) {
        authService.clearSession();
        void router.navigate(['/login'], {
          queryParams: { reason: SESSION_EXPIRED_REASON },
        });
      }

      return throwError(() => error);
    }),
  );
};
