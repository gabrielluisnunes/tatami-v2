import { Component, inject } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { HttpErrorResponse } from '@angular/common/http';
import { AuthService } from '../../../core/auth/auth.service';

@Component({
  selector: 'app-register',
  imports: [ReactiveFormsModule, RouterLink],
  templateUrl: './register.component.html',
  styleUrl: './register.component.scss',
})
export class RegisterComponent {
  private readonly formBuilder = inject(FormBuilder);
  private readonly authService = inject(AuthService);
  private readonly router = inject(Router);

  errorMessage = '';
  loading = false;

  readonly form = this.formBuilder.nonNullable.group({
    fullName: ['', [Validators.required, Validators.minLength(2)]],
    email: ['', [Validators.required, Validators.email]],
    password: ['', [Validators.required, Validators.minLength(8)]],
    confirmPassword: ['', [Validators.required, Validators.minLength(8)]],
  });

  submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const { fullName, email, password, confirmPassword } = this.form.getRawValue();

    if (password !== confirmPassword) {
      this.errorMessage = 'As senhas não conferem.';
      return;
    }

    this.errorMessage = '';
    this.loading = true;

    this.authService
      .register({
        fullName,
        email,
        password,
        role: 'admin',
      })
      .subscribe({
        next: () => this.router.navigateByUrl('/onboarding'),
        error: (error: HttpErrorResponse) => {
          this.loading = false;
          this.errorMessage =
            error.error?.error ?? 'Não foi possível criar a conta. Tente novamente.';
        },
        complete: () => {
          this.loading = false;
        },
      });
  }
}
