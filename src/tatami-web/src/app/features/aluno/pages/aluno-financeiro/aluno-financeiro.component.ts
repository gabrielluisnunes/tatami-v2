import { CurrencyPipe, DatePipe } from '@angular/common';
import { Component, DestroyRef, ElementRef, OnInit, ViewChild, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { Subscription } from 'rxjs';
import { FinancialService } from '../../../../core/financials/financial.service';
import { PixQrService } from '../../../../core/financials/pix-qr.service';
import { PixResponse, StudentFinancial, financialStatusLabels } from '../../../../core/financials/financial.models';

@Component({
  selector: 'app-aluno-financeiro',
  imports: [CurrencyPipe, DatePipe],
  templateUrl: './aluno-financeiro.component.html',
  styleUrl: './aluno-financeiro.component.scss',
})
export class AlunoFinanceiroComponent implements OnInit {
  private readonly financials = inject(FinancialService);
  private readonly qr = inject(PixQrService);
  private readonly destroyRef = inject(DestroyRef);
  private pixRequest?: Subscription;
  private listRequest?: Subscription;
  private pixVersion = 0;
  @ViewChild('pixDialog', { static: true }) private pixDialog!: ElementRef<HTMLDialogElement>;

  readonly charges = signal<StudentFinancial[]>([]);
  readonly loading = signal(false);
  readonly error = signal('');
  readonly success = signal('');
  readonly busyId = signal<string | null>(null);
  readonly selected = signal<StudentFinancial | null>(null);
  readonly pix = signal<PixResponse | null>(null);
  readonly qrUrl = signal('');
  readonly pixError = signal('');
  readonly pixLoading = signal(false);
  readonly copied = signal(false);
  readonly labels = financialStatusLabels;

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.listRequest?.unsubscribe();
    this.loading.set(true);
    this.error.set('');
    this.listRequest = this.financials.listMine().pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (charges) => {
        this.charges.set(charges);
        this.loading.set(false);
      },
      error: (error) => {
        this.error.set(error?.error?.error ?? 'Não foi possível carregar as mensalidades.');
        this.loading.set(false);
      },
    });
  }

  payable(charge: StudentFinancial): boolean {
    return charge.status === 'pending' || charge.status === 'overdue';
  }

  openPix(charge: StudentFinancial): void {
    if (!this.payable(charge) || this.busyId()) return;
    this.pixRequest?.unsubscribe();
    const version = ++this.pixVersion;
    this.selected.set(charge);
    this.pix.set(null);
    this.qrUrl.set('');
    this.copied.set(false);
    this.pixError.set('');
    this.pixLoading.set(true);
    this.pixDialog.nativeElement.showModal();
    this.pixRequest = this.financials.getPix(charge.id).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (pix) => {
        if (version !== this.pixVersion) return;
        this.pix.set(pix);
        this.pixLoading.set(false);
        void this.qr.dataUrl(pix.payload).then((url) => {
          if (version === this.pixVersion && !this.destroyRef.destroyed) this.qrUrl.set(url);
        }).catch(() => {
          if (version === this.pixVersion && !this.destroyRef.destroyed)
            this.pixError.set('Não foi possível gerar o QR. Use o código copia-e-cola.');
        });
      },
      error: (error) => {
        this.pixLoading.set(false);
        this.pixError.set(error?.error?.error ?? 'Não foi possível gerar o PIX.');
      },
    });
  }

  closePix(): void {
    this.pixDialog.nativeElement.close();
    this.resetPix();
  }

  resetPix(): void {
    this.pixVersion++;
    this.pixRequest?.unsubscribe();
    this.selected.set(null);
    this.pix.set(null);
    this.qrUrl.set('');
    this.copied.set(false);
  }

  async copyPix(): Promise<void> {
    const pix = this.pix();
    if (!pix) return;
    try {
      await navigator.clipboard.writeText(pix.payload);
      if (this.pix() === pix) this.copied.set(true);
    } catch {
      this.pixError.set('Não foi possível copiar. Selecione o código e copie manualmente.');
    }
  }

  markAwaiting(charge: StudentFinancial): void {
    if (!this.payable(charge) || this.busyId()) return;
    if (!window.confirm('Você já realizou o pagamento? A academia precisará confirmar o recebimento.')) return;
    this.busyId.set(charge.id);
    this.error.set('');
    this.success.set('');
    this.financials.markAwaiting(charge.id).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: () => {
        this.charges.update((charges) => charges.map((item) => item.id === charge.id
          ? { ...item, status: 'aguardando_confirmacao' } : item));
        this.busyId.set(null);
        this.closePix();
        this.success.set('Pagamento informado. Aguarde a confirmação da academia.');
        this.load();
      },
      error: (error) => {
        this.busyId.set(null);
        const message = error?.error?.error ?? 'Não foi possível informar o pagamento.';
        this.error.set(message);
        this.pixError.set(message);
      },
    });
  }
}