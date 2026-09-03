import { Component, inject } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { HttpErrorResponse } from '@angular/common/http';
import { AcademyService } from '../../core/academy/academy.service';
import { OnboardingResponse, SPORT_OPTIONS } from '../../core/academy/academy.models';
import { AuthService } from '../../core/auth/auth.service';

@Component({
  selector: 'app-onboarding',
  imports: [ReactiveFormsModule],
  templateUrl: './onboarding.component.html',
  styleUrl: './onboarding.component.scss',
})
export class OnboardingComponent {
  private readonly formBuilder = inject(FormBuilder);
  private readonly academyService = inject(AcademyService);
  private readonly authService = inject(AuthService);
  private readonly router = inject(Router);

  readonly sports = SPORT_OPTIONS;
  readonly ownerName = this.authService.getUser()?.fullName ?? '';
  errorMessage = '';
  loading = false;

  readonly form = this.formBuilder.nonNullable.group({
    academyName: ['', [Validators.required, Validators.minLength(2)]],
    sport: ['jiu-jitsu', Validators.required],
    monthlyPrice: [0, [Validators.required, Validators.min(0)]],
  });

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
        this.loading = false;
        this.router.navigateByUrl('/dashboard');
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
}
