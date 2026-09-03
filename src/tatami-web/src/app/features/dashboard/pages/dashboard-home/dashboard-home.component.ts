import { Component, inject, OnInit, signal } from '@angular/core';
import { Academy } from '../../../../core/academy/academy.models';
import { AcademyService } from '../../../../core/academy/academy.service';
import { AuthService } from '../../../../core/auth/auth.service';

@Component({
  selector: 'app-dashboard-home',
  templateUrl: './dashboard-home.component.html',
  styleUrl: './dashboard-home.component.scss',
})
export class DashboardHomeComponent implements OnInit {
  private readonly authService = inject(AuthService);
  private readonly academyService = inject(AcademyService);

  readonly adminName = this.authService.getUser()?.fullName ?? 'Admin';
  readonly academy = signal<Academy | null>(null);
  readonly errorMessage = signal('');

  readonly cards = [
    { label: 'Total de alunos', value: '—', hint: 'Issue #5' },
    { label: 'Cobranças do mês', value: '—', hint: 'Issue #21' },
    { label: 'Inadimplentes', value: '—', hint: 'Issue #21' },
    { label: 'Presenças', value: '—', hint: 'Issue #26' },
  ];

  ngOnInit(): void {
    this.academyService.getMyAcademy().subscribe({
      next: academy => this.academy.set(academy),
      error: () => this.errorMessage.set('Não foi possível carregar a academia.'),
    });
  }
}
