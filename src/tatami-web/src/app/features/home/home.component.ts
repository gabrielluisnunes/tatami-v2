import { Component, inject, OnInit, signal } from '@angular/core';
import { Router } from '@angular/router';
import { Academy } from '../../core/academy/academy.models';
import { AcademyService } from '../../core/academy/academy.service';
import { AuthService } from '../../core/auth/auth.service';

@Component({
  selector: 'app-home',
  template: `
    <section>
      <h1>Tatami v2</h1>
      @if (academy(); as academyData) {
        <p>Academia: <strong>{{ academyData.name }}</strong></p>
        <p>Esporte: {{ academyData.sport }} · R$ {{ academyData.monthlyPrice }}</p>
        <p>Status: {{ academyData.subscriptionStatus }}</p>
      } @else if (errorMessage()) {
        <p class="error">{{ errorMessage() }}</p>
      } @else {
        <p>Carregando academia...</p>
      }
      <button type="button" (click)="logout()">Sair</button>
    </section>
  `,
  styles: `
    :host {
      display: block;
      padding: 2rem;
      font-family: system-ui, sans-serif;
    }

    .error {
      color: #dc2626;
    }
  `,
})
export class HomeComponent implements OnInit {
  private readonly authService = inject(AuthService);
  private readonly academyService = inject(AcademyService);
  private readonly router = inject(Router);

  readonly academy = signal<Academy | null>(null);
  readonly errorMessage = signal('');

  ngOnInit(): void {
    if (this.authService.needsOnboarding()) {
      return;
    }

    this.academyService.getMyAcademy().subscribe({
      next: academy => this.academy.set(academy),
      error: () => this.errorMessage.set('Não foi possível carregar a academia.'),
    });
  }

  logout(): void {
    this.authService.logout();
    this.router.navigateByUrl('/login');
  }
}
