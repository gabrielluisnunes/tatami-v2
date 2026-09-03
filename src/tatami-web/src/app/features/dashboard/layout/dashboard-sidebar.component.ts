import { Component, input, output } from '@angular/core';
import { RouterLink } from '@angular/router';
import {
  academiaItems,
  contaItems,
  DashboardNavItem,
  gestaoItems,
} from '../config/dashboard-nav.config';
import { DashboardIconComponent } from './dashboard-icon.component';

@Component({
  selector: 'app-dashboard-sidebar',
  imports: [RouterLink, DashboardIconComponent],
  template: `
    <div class="brand">
      <span class="logo">Tatami</span>
      <p class="academy-name">{{ academyName() }}</p>
    </div>

    <nav class="nav">
      <p class="nav-group-label">Gestão</p>
      @for (item of gestaoItems; track item.path) {
        <a
          [routerLink]="item.path"
          [class.active]="isActive(item)"
          (click)="navigated.emit()"
        >
          <app-dashboard-icon [name]="item.icon" />
          {{ item.label }}
        </a>
      }

      <p class="nav-group-label spaced">Academia</p>
      @for (item of academiaItems; track item.path) {
        <a
          [routerLink]="item.path"
          [class.active]="isActive(item)"
          (click)="navigated.emit()"
        >
          <app-dashboard-icon [name]="item.icon" />
          {{ item.label }}
        </a>
      }

      <p class="nav-group-label spaced">Conta</p>
      @for (item of contaItems; track item.path) {
        <a
          [routerLink]="item.path"
          [class.active]="isActive(item)"
          (click)="navigated.emit()"
        >
          <app-dashboard-icon [name]="item.icon" />
          {{ item.label }}
        </a>
      }
      <button type="button" class="logout" (click)="logout.emit()">Sair</button>
    </nav>

    <div class="footer">
      <span class="avatar">{{ initial() }}</span>
      <span class="admin-name">{{ adminName() }}</span>
    </div>
  `,
  styleUrl: './dashboard-sidebar.component.scss',
})
export class DashboardSidebarComponent {
  readonly academyName = input('');
  readonly adminName = input('Admin');
  readonly currentPath = input('');
  readonly navigated = output<void>();
  readonly logout = output<void>();

  readonly gestaoItems = gestaoItems;
  readonly academiaItems = academiaItems;
  readonly contaItems = contaItems;

  isActive(item: DashboardNavItem): boolean {
    return item.path === '/dashboard'
      ? this.currentPath() === '/dashboard'
      : this.currentPath().startsWith(item.path);
  }

  initial(): string {
    return this.adminName().charAt(0).toUpperCase();
  }
}
