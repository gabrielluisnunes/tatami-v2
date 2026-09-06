export interface Academy {
  id: string;
  name: string;
  sport: string;
  monthlyPrice: number;
  subscriptionStatus: string;
  ownerId: string;
  plan?: string | null;
  stripeCustomerId?: string | null;
  trialEndsAt?: string | null;
}

export interface CreateOnboardingRequest {
  academyName: string;
  sport: string;
  monthlyPrice: number;
}

export interface OnboardingResponse {
  academy: Academy;
  auth: import('../auth/auth.models').AuthResponse;
}

export interface CheckoutSessionRequest {
  priceId: string;
  academyId: string;
}

export interface StripeSessionResponse {
  url: string;
}

export interface UpdateAcademyRequest {
  name: string;
  sport: string;
  monthlyPrice: number;
}

export const SPORT_OPTIONS = [
  { value: 'jiu-jitsu', label: 'Jiu-Jitsu' },
  { value: 'muay thai', label: 'Muay Thai' },
  { value: 'boxe', label: 'Boxe' },
  { value: 'misto', label: 'Misto' },
] as const;

export function hasCompletedCheckout(academy: Pick<Academy, 'plan' | 'stripeCustomerId'>): boolean {
  return Boolean(academy.plan && academy.stripeCustomerId);
}
