import { Component, OnInit, inject } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { HttpErrorResponse } from '@angular/common/http';
import { environment } from '../../../environments/environment';
import { AcademyService } from '../../core/academy/academy.service';
import {
  OnboardingResponse,
  SPORT_OPTIONS,
  hasCompletedCheckout,
} from '../../core/academy/academy.models';
import { SAAS_PLAN_CARDS, SaasPlanCard } from '../../core/academy/saas-plans';
import { AuthService } from '../../core/auth/auth.service';

@Component({
  selector: 'app-onboarding',
  imports: [ReactiveFormsModule],
  templateUrl: './onboarding.component.html',
  styleUrl: './onboarding.component.scss',
})
export class OnboardingComponent implements OnInit {
  private readonly formBuilder = inject(FormBuilder);
  private readonly academyService = inject(AcademyService);
  private readonly authService = inject(AuthService);
  private readonly router = inject(Router);

  readonly sports = SPORT_OPTIONS;
  readonly plans = SAAS_PLAN_CARDS;
  readonly ownerName = this.authService.getUser()?.fullName ?? '';

  step: 1 | 2 = 1;
  academyId: string | null = null;
  errorMessage = '';
  loading = false;
  resuming = true;

  readonly form = this.formBuilder.nonNullable.group({
    academyName: ['', [Validators.required, Validators.minLength(2)]],
    sport: ['jiu-jitsu', Validators.required],
    monthlyPrice: [0, [Validators.required, Validators.min(0)]],
  });

  ngOnInit(): void {
    const user = this.authService.getUser();
    if (!user?.academyId) {
      this.resuming = false;
      return;
    }

    if (!environment.enforceSubscription) {
      this.router.navigateByUrl('/dashboard');
      return;
    }

    this.academyService.getMyAcademy().subscribe({
      next: academy => {
        if (hasCompletedCheckout(academy)) {
          this.router.navigateByUrl('/dashboard');
          return;
        }

        this.academyId = academy.id;
        this.step = 2;
        this.resuming = false;
      },
      error: () => {
        this.resuming = false;
      },
    });
  }

  submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.errorMessage = '';
    this.loading = true;

    this.academyService.completeOnboarding(this.form.getRawValue()).subscribe({
      next: (response: OnboardingResponse) => {
        this.authService.updateSession(response.auth);
        this.academyId = response.academy.id;
        this.loading = false;

        if (!environment.enforceSubscription) {
          this.router.navigateByUrl('/dashboard');
          return;
        }

        this.step = 2;
      },
      error: (error: HttpErrorResponse) => {
        this.loading = false;
        if (error.status === 401) {
          this.authService.clearSession();
          this.router.navigateByUrl('/login');
          return;
        }

        this.errorMessage =
          error.error?.error ?? 'Não foi possível criar a academia.';
      },
    });
  }

  selectPlan(plan: SaasPlanCard): void {
    if (!this.academyId) {
      this.errorMessage = 'Academia não encontrada. Recarregue a página.';
      return;
    }

    this.errorMessage = '';
    this.loading = true;

    this.academyService
      .createCheckoutSession({
        priceId: plan.priceId,
        academyId: this.academyId,
      })
      .subscribe({
        next: response => {
          window.location.href = response.url;
        },
        error: (error: HttpErrorResponse) => {
          this.loading = false;
          this.errorMessage =
            error.error?.error ?? 'Não foi possível iniciar o checkout.';
        },
      });
  }
}
