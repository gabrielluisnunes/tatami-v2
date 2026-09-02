import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../environments/environment';
import { Academy, CreateOnboardingRequest, OnboardingResponse } from './academy.models';

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
}
