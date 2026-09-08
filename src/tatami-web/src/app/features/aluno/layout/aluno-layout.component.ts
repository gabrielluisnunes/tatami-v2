import { Component, inject } from '@angular/core';
import { NavigationEnd, Router, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { toSignal } from '@angular/core/rxjs-interop';
import { filter, map, startWith } from 'rxjs';
import { AuthService } from '../../../core/auth/auth.service';

@Component({
  selector: 'app-aluno-layout',
  imports: [RouterOutlet, RouterLink, RouterLinkActive],
  templateUrl: './aluno-layout.component.html',
  styleUrl: './aluno-layout.component.scss',
})
export class AlunoLayoutComponent {
  private readonly authService = inject(AuthService);
  private readonly router = inject(Router);

  readonly hideChrome = toSignal(
    this.router.events.pipe(
      filter((event): event is NavigationEnd => event instanceof NavigationEnd),
      map(() => this.router.url.includes('/completar-perfil')),
      startWith(this.router.url.includes('/completar-perfil')),
    ),
    { initialValue: this.router.url.includes('/completar-perfil') },
  );

  readonly userName = this.authService.getUser()?.fullName ?? 'Aluno';

  logout(): void {
    this.authService.logout();
    void this.router.navigateByUrl('/login');
  }
}
