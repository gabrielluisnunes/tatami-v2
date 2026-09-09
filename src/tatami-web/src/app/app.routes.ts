import { Routes } from '@angular/router';
import { authGuard } from './core/guards/auth.guard';
import { guestGuard } from './core/guards/guest.guard';
import {
  onboardingCompleteGuard,
  onboardingRequiredGuard,
} from './core/guards/onboarding.guard';
import { adminGuard, professorGuard } from './core/guards/role.guard';
import { subscriptionRequiredGuard } from './core/guards/subscription-required.guard';
import { LoginComponent } from './features/auth/login/login.component';
import { RegisterComponent } from './features/auth/register/register.component';
import { OnboardingComponent } from './features/onboarding/onboarding.component';
import { ProfessorHomeComponent } from './features/professor/professor-home.component';

export const routes: Routes = [
  { path: 'login', component: LoginComponent, canActivate: [guestGuard] },
  { path: 'register', component: RegisterComponent, canActivate: [guestGuard] },
  {
    path: 'onboarding',
    component: OnboardingComponent,
    canActivate: [authGuard, onboardingCompleteGuard],
  },
  {
    path: 'professor',
    component: ProfessorHomeComponent,
    canActivate: [authGuard, professorGuard],
  },
  {
    path: 'aluno',
    loadChildren: () =>
      import('./features/aluno/aluno.routes').then(m => m.alunoRoutes),
  },
  {
    path: 'dashboard',
    loadChildren: () =>
      import('./features/dashboard/dashboard.routes').then(m => m.dashboardRoutes),
    canActivate: [
      authGuard,
      adminGuard,
      onboardingRequiredGuard,
      subscriptionRequiredGuard,
    ],
  },
  { path: '', redirectTo: 'dashboard', pathMatch: 'full' },
  { path: '**', redirectTo: 'dashboard' },
];
