import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../environments/environment';
import {
  Academy,
  CheckoutSessionRequest,
  CreateOnboardingRequest,
  OnboardingResponse,
  StripeSessionResponse,
} from './academy.models';

@Injectable({ providedIn: 'root' })
export class AcademyService {
  constructor(private readonly http: HttpClient) {}

  completeOnboarding(request: CreateOnboardingRequest) {
    return this.http.post<OnboardingResponse>(
      `${environment.apiUrl}/api/onboarding`,
      request,
    );
  }

  getMyAcademy() {
    return this.http.get<Academy>(`${environment.apiUrl}/api/academies/me`);
  }

  createCheckoutSession(request: CheckoutSessionRequest) {
    return this.http.post<StripeSessionResponse>(
      `${environment.apiUrl}/api/stripe/checkout-session`,
      request,
    );
  }

  createPortalSession() {
    return this.http.post<StripeSessionResponse>(
      `${environment.apiUrl}/api/stripe/portal-session`,
      {},
    );
  }
}
