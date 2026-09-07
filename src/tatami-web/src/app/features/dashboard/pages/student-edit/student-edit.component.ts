import { Component, OnInit, inject } from '@angular/core';
import { FormArray, FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { HttpErrorResponse } from '@angular/common/http';
import { ViaCepService } from '../../../../core/address/viacep.service';
import {
  BELT_OPTIONS,
  STUDENT_SPORT_OPTIONS,
  StudentSportInput,
} from '../../../../core/students/student.models';
import { StudentService } from '../../../../core/students/student.service';

@Component({
  selector: 'app-student-edit',
  imports: [ReactiveFormsModule, RouterLink],
  templateUrl: './student-edit.component.html',
  styleUrl: './student-edit.component.scss',
})
export class StudentEditComponent implements OnInit {
  private readonly formBuilder = inject(FormBuilder);
  private readonly studentService = inject(StudentService);
  private readonly viaCepService = inject(ViaCepService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);

  readonly sportsOptions = STUDENT_SPORT_OPTIONS;
  readonly beltOptions = BELT_OPTIONS;

  studentId = '';
  email = '';
  loading = true;
  saving = false;
  cepLoading = false;
  cepError = '';
  errorMessage = '';

  readonly form = this.formBuilder.nonNullable.group({
    fullName: ['', [Validators.required, Validators.minLength(2)]],
    phone: [''],
    emergencyPhone: [''],
    birthDate: [''],
    cep: [''],
    address: [''],
    neighborhood: [''],
    city: [''],
    state: [''],
    sports: this.formBuilder.array([this.createSportGroup()]),
  });

  get sports(): FormArray {
    return this.form.controls.sports;
  }

  onCepInput(event: Event): void {
    const input = event.target as HTMLInputElement;
    const formatted = this.viaCepService.formatCep(input.value);
    this.form.controls.cep.setValue(formatted, { emitEvent: false });
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
        this.form.patchValue({
          address: data.logradouro ?? '',
          neighborhood: data.bairro ?? '',
          city: data.localidade ?? '',
          state: data.uf ?? '',
        });
      },
      error: () => {
        this.cepLoading = false;
        this.cepError = 'CEP não encontrado. Verifique e tente novamente.';
        this.form.patchValue({
          address: '',
          neighborhood: '',
          city: '',
          state: '',
        });
      },
    });
  }

  ngOnInit(): void {
    this.studentId = this.route.snapshot.paramMap.get('id') ?? '';
    if (!this.studentId) {
      this.loading = false;
      this.errorMessage = 'Aluno não encontrado.';
      return;
    }

    this.studentService.getById(this.studentId).subscribe({
      next: student => {
        this.email = student.email;
        this.sports.clear();
        for (const sport of student.sports) {
          this.sports.push(
            this.formBuilder.nonNullable.group({
              sport: [sport.sport, Validators.required],
              belt: [sport.belt ?? 'branca'],
              degree: [sport.degree, [Validators.min(0), Validators.max(4)]],
            }),
          );
        }
        if (this.sports.length === 0) {
          this.sports.push(this.createSportGroup());
        }

        this.form.patchValue({
          fullName: student.fullName,
          phone: student.phone ?? '',
          emergencyPhone: student.emergencyPhone ?? '',
          birthDate: student.birthDate ?? '',
          cep: student.cep ?? '',
          address: student.address ?? '',
          neighborhood: student.neighborhood ?? '',
          city: student.city ?? '',
          state: student.state ?? '',
        });
        this.loading = false;
      },
      error: (error: HttpErrorResponse) => {
        this.loading = false;
        this.errorMessage =
          error.error?.error ?? 'Não foi possível carregar o aluno.';
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

  submit(): void {
    if (this.form.invalid || this.sports.length === 0) {
      this.form.markAllAsTouched();
      return;
    }

    const value = this.form.getRawValue();
    const sports: StudentSportInput[] = value.sports.map(sport => ({
      sport: sport.sport,
      belt: sport.sport === 'boxe' ? null : sport.belt || null,
      degree: sport.sport === 'boxe' ? 0 : Number(sport.degree) || 0,
    }));

    this.saving = true;
    this.errorMessage = '';

    this.studentService
      .update(this.studentId, {
        fullName: value.fullName,
        phone: value.phone || null,
        emergencyPhone: value.emergencyPhone || null,
        birthDate: value.birthDate || null,
        cep: value.cep || null,
        address: value.address || null,
        neighborhood: value.neighborhood || null,
        city: value.city || null,
        state: value.state || null,
        sports,
      })
      .subscribe({
        next: () => {
          this.saving = false;
          this.router.navigateByUrl('/dashboard/alunos?updated=1');
        },
        error: (error: HttpErrorResponse) => {
          this.saving = false;
          this.errorMessage =
            error.error?.error ?? 'Não foi possível salvar o aluno.';
        },
      });
  }
}
