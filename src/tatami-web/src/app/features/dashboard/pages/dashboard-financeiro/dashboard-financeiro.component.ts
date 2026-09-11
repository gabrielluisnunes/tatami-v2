import { DatePipe } from '@angular/common';
import { Component, DestroyRef, ElementRef, OnInit, ViewChild, inject } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { AbstractControl, FormControl, FormGroup, FormsModule, ReactiveFormsModule, Validators } from '@angular/forms';
import { Observable, Subscription, finalize, switchMap } from 'rxjs';
import { FinancialOverview, FinancialRow, currentFinancialMonth, financialStatusLabels } from '../../../../core/financials/financial.models';
import { FinancialService } from '../../../../core/financials/financial.service';

function validAmount(control: AbstractControl) {
  const value = String(control.value ?? '').trim().replace(',', '.');
  return /^\d{1,8}(\.\d{1,2})?$/.test(value) && Number(value) > 0 && Number(value) <= 99999999.99
    ? null : { amount: true };
}

function paidAtTimestamp(value: string): number {
  if (!/^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}$/.test(value) || value.startsWith('0000')) return NaN;
  const time = Date.parse(`${value}:00-03:00`);
  return Number.isFinite(time) && new Date(time - 3 * 60 * 60 * 1000).toISOString().slice(0, 16) === value
    ? time : NaN;
}

function validPaidAt(control: AbstractControl) {
  const time = paidAtTimestamp(control.value ?? '');
  return Number.isFinite(time) && time <= Date.now() ? null : { paidAt: true };
}

@Component({
  selector: 'app-dashboard-financeiro',
  imports: [DatePipe, FormsModule, ReactiveFormsModule],
  templateUrl: './dashboard-financeiro.component.html',
  styleUrl: './dashboard-financeiro.component.scss',
})
export class DashboardFinanceiroComponent implements OnInit {
  private readonly financialService = inject(FinancialService);
  private readonly destroyRef = inject(DestroyRef);
  private readonly currency = new Intl.NumberFormat('pt-BR', { style: 'currency', currency: 'BRL' });
  private overviewRequest?: Subscription;
  private exportRequest?: Subscription;

  @ViewChild('manualDialog') manualDialog?: ElementRef<HTMLDialogElement>;

  readonly statusLabels = financialStatusLabels;
  readonly statuses = Object.keys(financialStatusLabels) as FinancialRow['status'][];
  readonly manualForm = new FormGroup({
    studentId: new FormControl('', { nonNullable: true, validators: [Validators.required] }),
    amount: new FormControl('', { nonNullable: true, validators: [validAmount] }),
    paidAt: new FormControl('', { nonNullable: true, validators: [validPaidAt] }),
  });

  selectedMonth = currentFinancialMonth();
  overview: FinancialOverview | null = null;
  search = '';
  statusFilter: FinancialRow['status'] | '' = '';
  loading = true;
  saving = false;
  exporting = false;
  errorMessage = '';
  successMessage = '';
  manualError = '';

  ngOnInit(): void {
    this.load();
  }

  get currentMonth(): string {
    return currentFinancialMonth();
  }

  get maxPaidAt(): string {
    return new Date(Date.now() - 3 * 60 * 60 * 1000).toISOString().slice(0, 16);
  }

  get canOpenManual(): boolean {
    return this.selectedMonth === this.currentMonth && !!this.overview && !this.loading && !this.saving;
  }

  get filteredStudents(): FinancialRow[] {
    const search = this.normalize(this.search.trim());
    return (this.overview?.students ?? []).filter(row =>
      (!this.statusFilter || row.status === this.statusFilter) &&
      (!search || this.normalize(`${row.studentName} ${row.email}`).includes(search)));
  }

  get overdueStudents(): FinancialRow[] {
    return this.filteredStudents.filter(row => row.status === 'overdue');
  }

  money(amount: number): string {
    return this.currency.format(amount);
  }

  monthLabel(month: string): string {
    const [year, number] = month.split('-');
    return `${number}/${year}`;
  }

  changeMonth(month: string): void {
    if (this.saving || !/^(?!0000)\d{4}-(0[1-9]|1[0-2])$/.test(month) || month === this.selectedMonth) return;
    this.closeManual();
    this.exportRequest?.unsubscribe();
    this.selectedMonth = month;
    this.load();
  }

  navigateMonth(offset: number): void {
    const [year, month] = this.selectedMonth.split('-').map(Number);
    const index = year * 12 + month - 1 + offset;
    if (index < 12 || index >= 10000 * 12) return;
    this.changeMonth(`${String(Math.floor(index / 12)).padStart(4, '0')}-${String(index % 12 + 1).padStart(2, '0')}`);
  }

  load(): void {
    if (this.saving) return;
    this.overviewRequest?.unsubscribe();
    this.loading = true;
    this.overview = null;
    this.errorMessage = '';
    this.successMessage = '';
    this.overviewRequest = this.financialService.overview(this.selectedMonth)
      .pipe(takeUntilDestroyed(this.destroyRef), finalize(() => this.loading = false))
      .subscribe({
        next: overview => this.overview = overview,
        error: () => this.errorMessage = 'Não foi possível carregar o financeiro. Tente novamente.',
      });
  }

  canMarkPaid(row: FinancialRow): boolean {
    return !!row.financialId && row.status !== 'paid' && row.status !== 'sem_cobranca';
  }

  markPaid(row: FinancialRow): void {
    if (this.saving || this.loading || !this.overview || !this.canMarkPaid(row)) return;
    if (!confirm(`Confirmar pagamento de ${row.studentName}, competência ${this.monthLabel(this.selectedMonth)}?`)) return;
    this.savePayment(this.financialService.markPaid(row.financialId!), 'Pagamento confirmado com sucesso.');
  }

  openManual(): void {
    if (!this.canOpenManual) return;
    this.manualForm.reset({ studentId: '', amount: '', paidAt: this.maxPaidAt });
    this.manualError = '';
    this.manualDialog?.nativeElement.showModal();
  }

  closeManual(): void {
    if (!this.saving) this.manualDialog?.nativeElement.close();
  }

  onDialogCancel(event: Event): void {
    if (this.saving) event.preventDefault();
  }

  submitManual(): void {
    if (this.saving || this.loading || !this.overview) return;
    this.manualForm.controls.paidAt.updateValueAndValidity();
    this.manualForm.markAllAsTouched();
    if (this.selectedMonth !== this.currentMonth) {
      this.manualError = 'Selecione a competência atual para registrar um pagamento manual.';
      return;
    }
    const value = this.manualForm.getRawValue();
    if (this.manualForm.invalid) return;
    if (!this.overview.students.some(row => row.studentId === value.studentId)) {
      this.manualError = 'Selecione um aluno da lista.';
      return;
    }
    this.savePayment(this.financialService.manualPayment({
      studentId: value.studentId,
      amount: Number(value.amount.trim().replace(',', '.')),
      paidAt: new Date(paidAtTimestamp(value.paidAt)).toISOString(),
    }), 'Pagamento manual registrado com sucesso.');
  }

  exportOverdue(): void {
    if (this.exporting || this.loading || this.saving || !this.overview) return;
    const month = this.selectedMonth;
    this.exporting = true;
    this.errorMessage = '';
    this.exportRequest = this.financialService.exportOverdue(month)
      .pipe(takeUntilDestroyed(this.destroyRef), finalize(() => this.exporting = false))
      .subscribe({
        next: blob => {
          const url = URL.createObjectURL(blob);
          const link = document.createElement('a');
          link.href = url;
          link.download = `inadimplentes-${month}.xlsx`;
          document.body.appendChild(link);
          try {
            link.click();
          } finally {
            link.remove();
            URL.revokeObjectURL(url);
          }
        },
        error: () => this.errorMessage = 'Não foi possível exportar os inadimplentes. Tente novamente.',
      });
  }

  private savePayment(request: Observable<void>, message: string): void {
    this.saving = true;
    this.errorMessage = '';
    this.manualError = '';
    this.successMessage = '';
    let recorded = false;
    request.pipe(
      switchMap(() => {
        recorded = true;
        this.manualDialog?.nativeElement.close();
        this.overview = null;
        this.loading = true;
        return this.financialService.overview(this.selectedMonth);
      }),
      takeUntilDestroyed(this.destroyRef),
      finalize(() => {
        this.saving = false;
        this.loading = false;
      }),
    ).subscribe({
      next: overview => {
        this.overview = overview;
        this.successMessage = message;
      },
      error: () => {
        const error = recorded
          ? 'O pagamento foi registrado, mas a atualização falhou. Recarregue os dados antes de realizar outra ação.'
          : 'Não foi possível registrar o pagamento. Confira os dados e tente novamente.';
        this.errorMessage = error;
        this.manualError = error;
      },
    });
  }

  private normalize(value: string): string {
    return value.normalize('NFD').replace(/[\u0300-\u036f]/g, '').toLowerCase();
  }
}