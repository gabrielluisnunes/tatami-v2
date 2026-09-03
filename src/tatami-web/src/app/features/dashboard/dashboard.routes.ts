import { Routes } from '@angular/router';
import { DashboardLayoutComponent } from './layout/dashboard-layout.component';
import { DashboardHomeComponent } from './pages/dashboard-home/dashboard-home.component';
import { DashboardStubComponent } from './pages/dashboard-stub.component';

export const dashboardRoutes: Routes = [
  {
    path: '',
    component: DashboardLayoutComponent,
    children: [
      { path: '', component: DashboardHomeComponent },
      {
        path: 'alunos',
        component: DashboardStubComponent,
        data: {
          title: 'Alunos',
          issue: 5,
          description: 'Cadastro, listagem e edição de alunos da academia.',
        },
      },
      {
        path: 'professores',
        component: DashboardStubComponent,
        data: {
          title: 'Professores',
          issue: 5,
          description: 'Gestão de professores e promoção a admin.',
        },
      },
      {
        path: 'financeiro',
        component: DashboardStubComponent,
        data: {
          title: 'Financeiro',
          issue: 21,
          description: 'Cobranças mensais, PIX e inadimplência dos alunos.',
        },
      },
      {
        path: 'checkins',
        component: DashboardStubComponent,
        data: {
          title: 'Check-ins',
          issue: 26,
          description: 'Histórico de presenças e check-in facial.',
        },
      },
      {
        path: 'turmas',
        component: DashboardStubComponent,
        data: {
          title: 'Turmas',
          issue: 22,
          description: 'Horários, esportes e professores das turmas.',
        },
      },
      {
        path: 'graduacoes',
        component: DashboardStubComponent,
        data: {
          title: 'Graduações',
          issue: 23,
          description: 'Progresso de faixas e registro de graduações.',
        },
      },
      {
        path: 'contratos',
        component: DashboardStubComponent,
        data: {
          title: 'Contratos',
          issue: 25,
          description: 'Contratos digitais e assinaturas dos alunos.',
        },
      },
      {
        path: 'comunicados',
        component: DashboardStubComponent,
        data: {
          title: 'Comunicados',
          issue: 27,
          description: 'Avisos em lote para alunos da academia.',
        },
      },
      {
        path: 'perfil',
        component: DashboardStubComponent,
        data: {
          title: 'Perfil',
          issue: 33,
          description: 'Dados do admin e configurações da academia.',
        },
      },
      {
        path: 'assinatura',
        component: DashboardStubComponent,
        data: {
          title: 'Assinatura',
          issue: 32,
          description: 'Plano SaaS da academia (Stripe). O bloqueio de trial sem checkout entra nesta issue.',
        },
      },
    ],
  },
];
