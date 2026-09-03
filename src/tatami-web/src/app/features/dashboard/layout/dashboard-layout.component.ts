import { Component, inject, OnInit, signal } from '@angular/core';
import { NavigationEnd, Router, RouterLink, RouterOutlet } from '@angular/router';
import { filter } from 'rxjs/operators';
import { AcademyService } from '../../../core/academy/academy.service';
import { AuthService } from '../../../core/auth/auth.service';
import {
  bottomNavItems,
  DashboardNavItem,
  getPageTitle,
  isNavItemActive,
} from '../config/dashboard-nav.config';
import { DashboardIconComponent } from './dashboard-icon.component';
import { DashboardSidebarComponent } from './dashboard-sidebar.component';

@Component({
  selector: 'app-dashboard-layout',
  imports: [RouterOutlet, RouterLink, DashboardIconComponent, DashboardSidebarComponent],
  templateUrl: './dashboard-layout.component.html',
  styleUrl: './dashboard-layout.component.scss',
})
export class DashboardLayoutComponent implements OnInit {
  private readonly authService = inject(AuthService);
  private readonly academyService = inject(AcademyService);
  private readonly router = inject(Router);

  readonly bottomNavItems = bottomNavItems;
  readonly adminName = this.authService.getUser()?.fullName ?? 'Admin';
  readonly academyName = signal('');
  readonly currentPath = signal(this.normalizedPath());
  readonly pageTitle = signal(getPageTitle(this.normalizedPath()));
  readonly drawerOpen = signal(false);

  ngOnInit(): void {
    this.academyService.getMyAcademy().subscribe({
      next: academy => this.academyName.set(academy.name),
      error: () => this.academyName.set(''),
    });

    this.router.events
      .pipe(filter((event): event is NavigationEnd => event instanceof NavigationEnd))
      .subscribe(event => {
        const path = event.urlAfterRedirects.split('?')[0];
        this.currentPath.set(path);
        this.pageTitle.set(getPageTitle(path));
        this.drawerOpen.set(false);
      });
  }

  isActive(item: DashboardNavItem): boolean {
    return isNavItemActive(this.currentPath(), item.path);
  }

  openDrawer(): void {
    this.drawerOpen.set(true);
  }

  closeDrawer(): void {
    this.drawerOpen.set(false);
  }

  logout(): void {
    this.authService.logout();
    this.router.navigateByUrl('/login');
  }

  private normalizedPath(): string {
    return this.router.url.split('?')[0];
  }
}
