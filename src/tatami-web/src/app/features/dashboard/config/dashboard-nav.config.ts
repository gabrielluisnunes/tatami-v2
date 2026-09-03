export interface DashboardNavItem {
  path: string;
  label: string;
  icon: DashboardIconName;
}

export type DashboardIconName =
  | 'painel'
  | 'alunos'
  | 'professores'
  | 'turmas'
  | 'checkins'
  | 'graduacoes'
  | 'contratos'
  | 'financeiro'
  | 'comunicados'
  | 'perfil'
  | 'menu';

export const gestaoItems: DashboardNavItem[] = [
  { path: '/dashboard', label: 'Painel', icon: 'painel' },
  { path: '/dashboard/alunos', label: 'Alunos', icon: 'alunos' },
  { path: '/dashboard/professores', label: 'Professores', icon: 'professores' },
  { path: '/dashboard/turmas', label: 'Turmas', icon: 'turmas' },
];

export const academiaItems: DashboardNavItem[] = [
  { path: '/dashboard/checkins', label: 'Check-ins', icon: 'checkins' },
  { path: '/dashboard/graduacoes', label: 'Graduações', icon: 'graduacoes' },
  { path: '/dashboard/contratos', label: 'Contratos', icon: 'contratos' },
  { path: '/dashboard/financeiro', label: 'Financeiro', icon: 'financeiro' },
  { path: '/dashboard/comunicados', label: 'Comunicados', icon: 'comunicados' },
];

export const contaItems: DashboardNavItem[] = [
  { path: '/dashboard/perfil', label: 'Perfil', icon: 'perfil' },
];

export const bottomNavItems: DashboardNavItem[] = [
  { path: '/dashboard', label: 'Painel', icon: 'painel' },
  { path: '/dashboard/alunos', label: 'Alunos', icon: 'alunos' },
  { path: '/dashboard/financeiro', label: 'Financeiro', icon: 'financeiro' },
  { path: '/dashboard/checkins', label: 'Check-ins', icon: 'checkins' },
];

export const pageTitles: Record<string, string> = {
  '/dashboard/comunicados': 'Comunicados',
  '/dashboard/professores': 'Professores',
  '/dashboard/graduacoes': 'Graduações',
  '/dashboard/financeiro': 'Financeiro',
  '/dashboard/contratos': 'Contratos',
  '/dashboard/checkins': 'Check-ins',
  '/dashboard/alunos': 'Alunos',
  '/dashboard/turmas': 'Turmas',
  '/dashboard/perfil': 'Perfil',
  '/dashboard/assinatura': 'Assinatura',
  '/dashboard': 'Painel',
};

export function getPageTitle(pathname: string): string {
  for (const [key, title] of Object.entries(pageTitles)) {
    if (key === '/dashboard' ? pathname === '/dashboard' : pathname.startsWith(key)) {
      return title;
    }
  }

  return 'Painel';
}

export function isNavItemActive(pathname: string, itemPath: string): boolean {
  return itemPath === '/dashboard'
    ? pathname === '/dashboard'
    : pathname.startsWith(itemPath);
}
