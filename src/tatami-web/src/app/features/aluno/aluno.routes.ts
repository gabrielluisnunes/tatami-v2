import { Routes } from '@angular/router';
import { authGuard } from '../../core/guards/auth.guard';
import { alunoGuard } from '../../core/guards/role.guard';
import { alunoProfileCompleteGuard } from '../../core/guards/aluno-profile-complete.guard';
import { AlunoLayoutComponent } from './layout/aluno-layout.component';
import { AlunoHomeComponent } from './pages/aluno-home/aluno-home.component';
import { CompletarPerfilComponent } from './pages/completar-perfil/completar-perfil.component';

export const alunoRoutes: Routes = [
  {
    path: '',
    component: AlunoLayoutComponent,
    canActivate: [authGuard, alunoGuard],
    children: [
      {
        path: '',
        component: AlunoHomeComponent,
        canActivate: [alunoProfileCompleteGuard],
      },
      {
        path: 'completar-perfil',
        component: CompletarPerfilComponent,
        canActivate: [alunoProfileCompleteGuard],
      },
    ],
  },
];
