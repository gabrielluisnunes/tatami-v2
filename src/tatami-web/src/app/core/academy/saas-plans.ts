import { environment } from '../../../environments/environment';

export interface SaasPlanCard {
  key: 'starter' | 'pro';
  name: string;
  price: string;
  description: string;
  features: string[];
  popular?: boolean;
  priceId: string;
}

export const SAAS_PLAN_CARDS: SaasPlanCard[] = [
  {
    key: 'starter',
    name: 'Starter',
    price: '79',
    description: 'Perfeito para academias em início de jornada.',
    features: [
      'Até 50 alunos ativos',
      'Gestão de treinos e presenças',
      'Histórico básico de faixas',
      'Suporte prioritário por email',
    ],
    priceId: environment.stripePriceIds.starter,
  },
  {
    key: 'pro',
    name: 'Pro',
    price: '175',
    description: 'Ideal para academias em pleno crescimento.',
    features: [
      'Alunos ilimitados',
      'Controle financeiro avançado',
      'Reconhecimento facial com IA',
      'WhatsApp integrado para alertas',
      'Suporte prioritário via WhatsApp',
    ],
    popular: true,
    priceId: environment.stripePriceIds.pro,
  },
];

export const PLAN_DISPLAY_NAMES: Record<string, string> = {
  starter: 'Starter (R$ 79/mês)',
  pro: 'Pro (R$ 175/mês)',
  'multi-unit': 'Multi-unit (R$ 299/mês)',
};
