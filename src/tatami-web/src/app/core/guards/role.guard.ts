import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthService } from '../auth/auth.service';

function createRoleGuard(expectedRole: string): CanActivateFn {
  return () => {
    const authService = inject(AuthService);
    const router = inject(Router);
    const user = authService.getUser();

    if (user?.role === expectedRole) {
      return true;
    }

    return router.createUrlTree([authService.getRoleHomeRoute()]);
  };
}

export const adminGuard = createRoleGuard('admin');
export const professorGuard = createRoleGuard('professor');
export const alunoGuard = createRoleGuard('aluno');
