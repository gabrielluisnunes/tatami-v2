import { Component, OnInit, inject } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, ValidatorFn, Validators } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { HttpErrorResponse } from '@angular/common/http';
import { Academy, PixKeyType, SPORT_OPTIONS } from '../../../../core/academy/academy.models';
import { AcademyService } from '../../../../core/academy/academy.service';
import { PLAN_DISPLAY_NAMES } from '../../../../core/academy/saas-plans';
import { AuthService } from '../../../../core/auth/auth.service';
import { environment } from '../../../../../environments/environment';

const pixValidator: ValidatorFn = control => {
  const key = (control.get('pixKey')?.value as string).trim();
  const type = control.get('pixKeyType')?.value as PixKeyType | '';
  if (!key && !type) {
    return null;
  }

  if (!key || !type) {
    return { pix: 'Informe a chave PIX e seu tipo, ou deixe ambos vazios.' };
  }

  const patterns: Record<PixKeyType, RegExp> = {
    celular: /^\+[1-9][0-9]{9,14}$/,
    email: /^[^\s@]+@[^\s@]+\.[^\s@]+$/,
    cpf: /^[0-9]{11}$/,
    cnpj: /^[0-9]{14}$/,
    aleatoria: /^[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}$/,
  };

  return key.length <= 254 && patterns[type]?.test(key)
    ? null
    : { pix: 'Chave PIX inválida para o tipo informado.' };
};

@Component({
  selector: 'app-dashboard-perfil',
  imports: [ReactiveFormsModule, RouterLink],
  templateUrl: './dashboard-perfil.component.html',
  styleUrl: './dashboard-perfil.component.scss',
})
export class DashboardPerfilComponent implements OnInit {
  private readonly formBuilder = inject(FormBuilder);
  private readonly authService = inject(AuthService);
  private readonly academyService = inject(AcademyService);

  readonly sports = SPORT_OPTIONS;
  readonly pixKeyTypes = [
    { value: 'celular', label: 'Celular' },
    { value: 'email', label: 'E-mail' },
    { value: 'cpf', label: 'CPF' },
    { value: 'cnpj', label: 'CNPJ' },
    { value: 'aleatoria', label: 'Aleatória' },
  ] as const;
  readonly enforceSubscription = environment.enforceSubscription;

  academy: Academy | null = null;
  loading = true;
  pageError = '';

  profileMessage = '';
  profileError = '';
  profileLoading = false;

  passwordMessage = '';
  passwordError = '';
  passwordLoading = false;

  academyMessage = '';
  academyError = '';
  academyLoading = false;

  readonly profileForm = this.formBuilder.nonNullable.group({
    fullName: ['', [Validators.required, Validators.minLength(2)]],
    email: [{ value: '', disabled: true }],
  });

  readonly passwordForm = this.formBuilder.nonNullable.group({
    currentPassword: ['', Validators.required],
    newPassword: ['', [Validators.required, Validators.minLength(8)]],
    confirmPassword: ['', Validators.required],
  });

  readonly academyForm = this.formBuilder.nonNullable.group({
    name: ['', [Validators.required, Validators.minLength(2)]],
    sport: ['jiu-jitsu', Validators.required],
    monthlyPrice: [0, [Validators.required, Validators.min(0)]],
    pixKey: [''],
    pixKeyType: this.formBuilder.nonNullable.control<PixKeyType | ''>(''),
  }, { validators: pixValidator });

  ngOnInit(): void {
    const user = this.authService.getUser();
    this.profileForm.patchValue({
      fullName: user?.fullName ?? '',
      email: user?.email ?? '',
    });

    this.academyService.getMyAcademy().subscribe({
      next: academy => {
        this.academy = academy;
        this.academyForm.patchValue({
          name: academy.name,
          sport: academy.sport,
          monthlyPrice: academy.monthlyPrice,
          pixKey: academy.pixKey ?? '',
          pixKeyType: academy.pixKeyType ?? '',
        });
        this.loading = false;
      },
      error: (error: HttpErrorResponse) => {
        this.loading = false;
        this.pageError =
          error.error?.error ?? 'Não foi possível carregar o perfil.';
      },
    });
  }

  get planLabel(): string {
    const plan = this.academy?.plan;
    if (!plan) {
      return 'Nenhum plano ativo';
    }

    return PLAN_DISPLAY_NAMES[plan] ?? plan;
  }

  get statusLabel(): string {
    const status = this.academy?.subscriptionStatus ?? 'trial';
    const labels: Record<string, string> = {
      trial: 'Trial local',
      trialing: 'Em teste',
      active: 'Ativo',
      past_due: 'Pagamento pendente',
      unpaid: 'Não pago',
      canceled: 'Cancelado',
      incomplete: 'Incompleto',
      incomplete_expired: 'Expirado',
    };

    return labels[status] ?? status;
  }

  saveProfile(): void {
    if (this.profileForm.invalid) {
      this.profileForm.markAllAsTouched();
      return;
    }

    this.profileMessage = '';
    this.profileError = '';
    this.profileLoading = true;

    this.authService
      .updateProfile({ fullName: this.profileForm.getRawValue().fullName })
      .subscribe({
        next: () => {
          this.profileLoading = false;
          this.profileMessage = 'Dados atualizados.';
        },
        error: (error: HttpErrorResponse) => {
          this.profileLoading = false;
          this.profileError =
            error.error?.error ?? 'Não foi possível salvar o perfil.';
        },
      });
  }

  savePassword(): void {
    const { currentPassword, newPassword, confirmPassword } =
      this.passwordForm.getRawValue();

    this.passwordMessage = '';
    this.passwordError = '';

    if (this.passwordForm.invalid) {
      this.passwordForm.markAllAsTouched();
      return;
    }

    if (newPassword !== confirmPassword) {
      this.passwordError = 'A confirmação da senha não confere.';
      return;
    }

    this.passwordLoading = true;

    this.authService
      .changePassword({ currentPassword, newPassword })
      .subscribe({
        next: response => {
          this.passwordLoading = false;
          this.passwordMessage = response.message;
          this.passwordForm.reset();
        },
        error: (error: HttpErrorResponse) => {
          this.passwordLoading = false;
          this.passwordError =
            error.error?.error ?? 'Não foi possível alterar a senha.';
        },
      });
  }

  saveAcademy(): void {
    if (this.academyForm.invalid) {
      this.academyForm.markAllAsTouched();
      return;
    }

    this.academyMessage = '';
    this.academyError = '';
    this.academyLoading = true;

    const value = this.academyForm.getRawValue();
    this.academyService.updateMyAcademy({
      ...value,
      pixKey: value.pixKey.trim() || null,
      pixKeyType: value.pixKeyType || null,
    }).subscribe({
      next: academy => {
        this.academy = academy;
        this.academyLoading = false;
        this.academyMessage = 'Academia atualizada.';
        window.dispatchEvent(new CustomEvent('tatami:academy-updated'));
      },
      error: (error: HttpErrorResponse) => {
        this.academyLoading = false;
        this.academyError =
          error.error?.error ?? 'Não foi possível salvar a academia.';
      },
    });
  }
}
