export interface Academy {
  id: string;
  name: string;
  sport: string;
  monthlyPrice: number;
  subscriptionStatus: string;
  ownerId: string;
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

export const SPORT_OPTIONS = [
  { value: 'jiu-jitsu', label: 'Jiu-Jitsu' },
  { value: 'muay thai', label: 'Muay Thai' },
  { value: 'boxe', label: 'Boxe' },
  { value: 'misto', label: 'Misto' },
] as const;
