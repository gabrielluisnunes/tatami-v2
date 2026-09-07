import { Component, inject } from '@angular/core';
import { FormArray, FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { HttpErrorResponse } from '@angular/common/http';
import { ViaCepService } from '../../../../core/address/viacep.service';
import {
  BELT_OPTIONS,
  STUDENT_SPORT_OPTIONS,
  StudentSportInput,
} from '../../../../core/students/student.models';
import { StudentService } from '../../../../core/students/student.service';

@Component({
  selector: 'app-student-enroll',
  imports: [ReactiveFormsModule, RouterLink],
  templateUrl: './student-enroll.component.html',
  styleUrl: './student-enroll.component.scss',
})
export class StudentEnrollComponent {
  private readonly formBuilder = inject(FormBuilder);
  private readonly studentService = inject(StudentService);
  private readonly viaCepService = inject(ViaCepService);
  private readonly router = inject(Router);

  readonly sportsOptions = STUDENT_SPORT_OPTIONS;
  readonly beltOptions = BELT_OPTIONS;
  step: 1 | 2 | 3 | 4 = 1;
  loading = false;
  cepLoading = false;
  cepError = '';
  errorMessage = '';
  temporaryPassword: string | null = null;
  createdEmail: string | null = null;

  readonly dataForm = this.formBuilder.nonNullable.group({
    fullName: ['', [Validators.required, Validators.minLength(2)]],
    email: ['', [Validators.required, Validators.email]],
    phone: [''],
    emergencyPhone: [''],
    birthDate: [''],
    cep: [''],
    address: [''],
    neighborhood: [''],
    city: [''],
    state: [''],
  });

  readonly sportsForm = this.formBuilder.nonNullable.group({
    sports: this.formBuilder.array([this.createSportGroup()]),
  });

  get sports(): FormArray {
    return this.sportsForm.controls.sports;
  }

  onCepInput(event: Event): void {
    const input = event.target as HTMLInputElement;
    const formatted = this.viaCepService.formatCep(input.value);
    this.dataForm.controls.cep.setValue(formatted, { emitEvent: false });
    input.value = formatted;
    this.cepError = '';

    const digits = formatted.replace(/\D/g, '');
    if (digits.length !== 8) {
      return;
    }

    this.cepLoading = true;
    this.viaCepService.lookup(digits).subscribe({
      next: data => {
        this.cepLoading = false;
        this.dataForm.patchValue({
          address: data.logradouro ?? '',
          neighborhood: data.bairro ?? '',
          city: data.localidade ?? '',
          state: data.uf ?? '',
        });
      },
      error: () => {
        this.cepLoading = false;
        this.cepError = 'CEP não encontrado. Verifique e tente novamente.';
        this.dataForm.patchValue({
          address: '',
          neighborhood: '',
          city: '',
          state: '',
        });
      },
    });
  }

  createSportGroup() {
    return this.formBuilder.nonNullable.group({
      sport: ['jiu-jitsu', Validators.required],
      belt: ['branca'],
      degree: [0, [Validators.min(0), Validators.max(4)]],
    });
  }

  addSport(): void {
    this.sports.push(this.createSportGroup());
  }

  removeSport(index: number): void {
    if (this.sports.length > 1) {
      this.sports.removeAt(index);
    }
  }

  isBoxe(index: number): boolean {
    return this.sports.at(index).get('sport')?.value === 'boxe';
  }

  goToStep2(): void {
    if (this.dataForm.invalid) {
      this.dataForm.markAllAsTouched();
      return;
    }
    this.errorMessage = '';
    this.step = 2;
  }

  goToStep3(): void {
    if (this.sports.invalid || this.sports.length === 0) {
      this.sportsForm.markAllAsTouched();
      return;
    }
    this.errorMessage = '';
    this.step = 3;
  }

  goToStep4(): void {
    this.step = 4;
  }

  back(): void {
    if (this.step > 1) {
      this.step = (this.step - 1) as 1 | 2 | 3 | 4;
    }
  }

  submit(): void {
    const data = this.dataForm.getRawValue();
    const sports: StudentSportInput[] = this.sports.getRawValue().map(sport => ({
      sport: sport.sport,
      belt: sport.sport === 'boxe' ? null : sport.belt || null,
      degree: sport.sport === 'boxe' ? 0 : Number(sport.degree) || 0,
    }));

    this.loading = true;
    this.errorMessage = '';

    this.studentService
      .enroll({
        fullName: data.fullName,
        email: data.email,
        phone: data.phone || null,
        emergencyPhone: data.emergencyPhone || null,
        birthDate: data.birthDate || null,
        cep: data.cep || null,
        address: data.address || null,
        neighborhood: data.neighborhood || null,
        city: data.city || null,
        state: data.state || null,
        sports,
      })
      .subscribe({
        next: response => {
          this.loading = false;
          this.createdEmail = response.student.email;
          this.temporaryPassword = response.temporaryPassword ?? null;
          if (!this.temporaryPassword) {
            this.router.navigateByUrl('/dashboard/alunos?created=1');
          }
        },
        error: (error: HttpErrorResponse) => {
          this.loading = false;
          this.errorMessage =
            error.error?.error ?? 'Não foi possível cadastrar o aluno.';
        },
      });
  }

  finishAfterPassword(): void {
    this.router.navigateByUrl('/dashboard/alunos?created=1');
  }
}
