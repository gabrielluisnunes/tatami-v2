import { Component, OnInit, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { DatePipe } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { Student } from '../../../../core/students/student.models';
import { StudentService } from '../../../../core/students/student.service';

@Component({
  selector: 'app-students-list',
  imports: [FormsModule, RouterLink, DatePipe],
  templateUrl: './students-list.component.html',
  styleUrl: './students-list.component.scss',
})
export class StudentsListComponent implements OnInit {
  private readonly studentService = inject(StudentService);
  private readonly route = inject(ActivatedRoute);

  students: Student[] = [];
  search = '';
  showInactive = false;
  loading = true;
  errorMessage = '';
  successMessage = '';
  actionLoadingId: string | null = null;

  ngOnInit(): void {
    if (this.route.snapshot.queryParamMap.get('created') === '1') {
      this.successMessage = 'Aluno cadastrado com sucesso.';
    }
    if (this.route.snapshot.queryParamMap.get('updated') === '1') {
      this.successMessage = 'Aluno atualizado com sucesso.';
    }

    this.load();
  }

  load(): void {
    this.loading = true;
    this.errorMessage = '';
    this.studentService
      .list({
        search: this.search.trim() || undefined,
        active: this.showInactive ? undefined : true,
      })
      .subscribe({
        next: students => {
          this.students = students;
          this.loading = false;
        },
        error: (error: HttpErrorResponse) => {
          this.loading = false;
          this.errorMessage =
            error.error?.error ?? 'Não foi possível carregar os alunos.';
        },
      });
  }

  onSearchSubmit(event: Event): void {
    event.preventDefault();
    this.load();
  }

  toggleInactive(): void {
    this.showInactive = !this.showInactive;
    this.load();
  }

  deactivate(student: Student): void {
    if (!confirm(`Desativar ${student.fullName}?`)) {
      return;
    }

    this.actionLoadingId = student.id;
    this.studentService.deactivate(student.id).subscribe({
      next: () => {
        this.actionLoadingId = null;
        this.load();
      },
      error: (error: HttpErrorResponse) => {
        this.actionLoadingId = null;
        this.errorMessage =
          error.error?.error ?? 'Não foi possível desativar o aluno.';
      },
    });
  }

  activate(student: Student): void {
    this.actionLoadingId = student.id;
    this.studentService.activate(student.id).subscribe({
      next: () => {
        this.actionLoadingId = null;
        this.load();
      },
      error: (error: HttpErrorResponse) => {
        this.actionLoadingId = null;
        this.errorMessage =
          error.error?.error ?? 'Não foi possível reativar o aluno.';
      },
    });
  }

  sportLabel(sport: string): string {
    if (sport === 'jiu-jitsu') return 'Jiu-Jitsu';
    if (sport === 'muay thai' || sport === 'muay-thai') return 'Muay Thai';
    if (sport === 'boxe') return 'Boxe';
    return sport;
  }
}
