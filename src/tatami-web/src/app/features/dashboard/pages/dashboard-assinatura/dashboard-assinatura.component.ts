import { Component, OnInit, inject } from '@angular/core';
import { DatePipe } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { Academy } from '../../../../core/academy/academy.models';
import { AcademyService } from '../../../../core/academy/academy.service';
import {
  PLAN_DISPLAY_NAMES,
  SAAS_PLAN_CARDS,
  SaasPlanCard,
} from '../../../../core/academy/saas-plans';

@Component({
  selector: 'app-dashboard-assinatura',
  imports: [DatePipe],
  templateUrl: './dashboard-assinatura.component.html',
  styleUrl: './dashboard-assinatura.component.scss',
})
export class DashboardAssinaturaComponent implements OnInit {
  private readonly academyService = inject(AcademyService);

  readonly plans = SAAS_PLAN_CARDS;
  academy: Academy | null = null;
  loading = true;
  actionLoading = false;
  errorMessage = '';
  showPlans = false;

  ngOnInit(): void {
    this.academyService.getMyAcademy().subscribe({
      next: academy => {
        this.academy = academy;
        this.showPlans = !academy.plan;
        this.loading = false;
      },
      error: (error: HttpErrorResponse) => {
        this.loading = false;
        this.errorMessage =
          error.error?.error ?? 'Não foi possível carregar a assinatura.';
      },
    });
  }

  get planLabel(): string {
    if (!this.academy?.plan) {
      return 'Nenhum plano ativo';
    }

    return PLAN_DISPLAY_NAMES[this.academy.plan] ?? this.academy.plan;
  }

  get statusLabel(): string {
    const status = this.academy?.subscriptionStatus ?? 'trial';
    const labels: Record<string, string> = {
      trial: 'Trial local',
      trialing: 'Em teste',
      active: 'Ativo',
      past_due: 'Pagamento pendente',
      unpaid: 'Não pago',
      canceled: 'Cancelado',
      incomplete: 'Incompleto',
      incomplete_expired: 'Expirado',
    };

    return labels[status] ?? status;
  }

  get isRestricted(): boolean {
    const status = this.academy?.subscriptionStatus ?? 'trial';
    return ['past_due', 'unpaid', 'incomplete', 'incomplete_expired'].includes(
      status,
    );
  }

  get isTrial(): boolean {
    const status = this.academy?.subscriptionStatus ?? 'trial';
    return status === 'trial' || status === 'trialing';
  }

  get hasStripeAccount(): boolean {
    return Boolean(this.academy?.stripeCustomerId);
  }

  selectPlan(plan: SaasPlanCard): void {
    if (!this.academy) {
      return;
    }

    this.errorMessage = '';
    this.actionLoading = true;

    this.academyService
      .createCheckoutSession({
        priceId: plan.priceId,
        academyId: this.academy.id,
      })
      .subscribe({
        next: response => {
          window.location.href = response.url;
        },
        error: (error: HttpErrorResponse) => {
          this.actionLoading = false;
          this.errorMessage =
            error.error?.error ?? 'Não foi possível iniciar o checkout.';
        },
      });
  }

  openPortal(): void {
    if (!this.hasStripeAccount) {
      this.errorMessage = 'Você ainda não possui uma assinatura vinculada.';
      return;
    }

    this.errorMessage = '';
    this.actionLoading = true;

    this.academyService.createPortalSession().subscribe({
      next: response => {
        window.location.href = response.url;
      },
      error: (error: HttpErrorResponse) => {
        this.actionLoading = false;
        this.errorMessage =
          error.error?.error ?? 'Não foi possível abrir o portal.';
      },
    });
  }
}
