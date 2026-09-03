import { Component, inject } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { ActivatedRoute, NavigationEnd, Router } from '@angular/router';
import { filter, map, startWith } from 'rxjs/operators';

export interface DashboardStubData {
  title: string;
  issue: number;
  description: string;
}

@Component({
  selector: 'app-dashboard-stub',
  template: `
    <section class="page">
      <h2>{{ data()?.title }}</h2>
      <p>{{ data()?.description }}</p>
      <p class="hint">
        Funcionalidade em construção —
        <a
          [href]="'https://github.com/gabrielluisnunes/tatami-v2/issues/' + data()?.issue"
          target="_blank"
          rel="noreferrer"
        >
          issue #{{ data()?.issue }}
        </a>
      </p>
    </section>
  `,
  styles: `
    .page {
      max-width: 40rem;
      padding: 1.5rem;
      background: #fff;
      border: 1px solid #e4e4e7;
      border-radius: 0.75rem;
    }

    h2 {
      margin: 0 0 0.5rem;
      font-size: 1.25rem;
    }

    p {
      margin: 0 0 0.75rem;
      color: #52525b;
    }

    .hint {
      margin: 0;
      font-size: 0.875rem;
      color: #71717a;
    }

    a {
      color: #18181b;
      font-weight: 600;
    }
  `,
})
export class DashboardStubComponent {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);

  readonly data = toSignal(
    this.router.events.pipe(
      filter((event): event is NavigationEnd => event instanceof NavigationEnd),
      startWith(null),
      map(() => this.route.snapshot.data as DashboardStubData),
    ),
  );
}
