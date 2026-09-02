import { Component, inject } from '@angular/core';
import { Router } from '@angular/router';
import { AuthService } from '../../core/auth/auth.service';

@Component({
  selector: 'app-professor-home',
  template: `
    <section>
      <h1>Área do professor</h1>
      <p>Em breve: check-in e turmas.</p>
      <button type="button" (click)="logout()">Sair</button>
    </section>
  `,
  styles: `
    :host {
      display: block;
      padding: 2rem;
      font-family: system-ui, sans-serif;
    }
  `,
})
export class ProfessorHomeComponent {
  private readonly authService = inject(AuthService);
  private readonly router = inject(Router);

  logout(): void {
    this.authService.logout();
    this.router.navigateByUrl('/login');
  }
}
