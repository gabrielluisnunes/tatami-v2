import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { catchError, map, of } from 'rxjs';
import { StudentProfileService } from '../students/student-profile.service';

export const alunoProfileCompleteGuard: CanActivateFn = (_route, state) => {
  const profileService = inject(StudentProfileService);
  const router = inject(Router);
  const isCompletar = state.url.includes('/aluno/completar-perfil');

  return profileService.getMe().pipe(
    map(profile => {
      if (!profile.isProfileComplete && !isCompletar) {
        return router.createUrlTree(['/aluno/completar-perfil']);
      }

      if (profile.isProfileComplete && isCompletar) {
        return router.createUrlTree(['/aluno']);
      }

      return true;
    }),
    catchError(() => {
      if (!isCompletar) {
        return of(router.createUrlTree(['/aluno/completar-perfil']));
      }
      return of(true);
    }),
  );
};
