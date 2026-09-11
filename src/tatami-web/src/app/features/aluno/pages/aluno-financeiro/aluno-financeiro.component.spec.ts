import { ComponentFixture, TestBed, fakeAsync, flushMicrotasks } from '@angular/core/testing';
import { Subject, of, throwError } from 'rxjs';
import { FinancialService } from '../../../../core/financials/financial.service';
import { PixQrService } from '../../../../core/financials/pix-qr.service';
import { PixResponse, StudentFinancial } from '../../../../core/financials/financial.models';
import { AlunoFinanceiroComponent } from './aluno-financeiro.component';

describe('AlunoFinanceiroComponent', () => {
  let fixture: ComponentFixture<AlunoFinanceiroComponent>;
  let component: AlunoFinanceiroComponent;
  let service: jasmine.SpyObj<FinancialService>;
  let qr: jasmine.SpyObj<PixQrService>;
  const pending: StudentFinancial = {
    id: 'charge', amount: 150, dueDate: '2026-09-10', referenceMonth: '2026-09-01', status: 'pending', paidAt: null,
  };
  const pix: PixResponse = { financialId: pending.id, amount: 150, payload: 'brcode' };

  beforeEach(async () => {
    service = jasmine.createSpyObj<FinancialService>('FinancialService', ['listMine', 'getPix', 'markAwaiting']);
    qr = jasmine.createSpyObj<PixQrService>('PixQrService', ['dataUrl']);
    service.listMine.and.returnValue(of([pending]));
    service.getPix.and.returnValue(of(pix));
    service.markAwaiting.and.returnValue(of(void 0));
    qr.dataUrl.and.resolveTo('data:image/png;base64,test');
    await TestBed.configureTestingModule({
      imports: [AlunoFinanceiroComponent],
      providers: [{ provide: FinancialService, useValue: service }, { provide: PixQrService, useValue: qr }],
    }).compileComponents();
    fixture = TestBed.createComponent(AlunoFinanceiroComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
    spyOn(window, 'confirm').and.returnValue(true);
  });

  it('lista mensalidades e bloqueia ações para paga/aguardando', () => {
    expect(fixture.nativeElement.textContent).toContain('Pendente');
    expect(component.payable(pending)).toBeTrue();
    expect(component.payable({ ...pending, status: 'overdue' })).toBeTrue();
    for (const status of ['paid', 'aguardando_confirmacao'] as const) {
      component.openPix({ ...pending, status });
      component.markAwaiting({ ...pending, status });
    }
    expect(service.getPix).not.toHaveBeenCalled();
    expect(service.markAwaiting).not.toHaveBeenCalled();
  });

  it('gera QR do payload retornado pela API', fakeAsync(() => {
    component.openPix(pending);
    flushMicrotasks();
    expect(component.pix()?.payload).toBe('brcode');
    expect(qr.dataUrl).toHaveBeenCalledWith('brcode');
    expect(component.qrUrl()).toBe('data:image/png;base64,test');
    component.closePix();
  }));

  it('preserva copia-e-cola quando QR falha', fakeAsync(() => {
    qr.dataUrl.and.rejectWith(new Error('QR'));
    component.openPix(pending);
    flushMicrotasks();
    expect(component.pix()?.payload).toBe('brcode');
    expect(component.pixError()).toContain('copia-e-cola');
    component.closePix();
  }));

  it('ignora QR de um modal fechado', fakeAsync(() => {
    let resolve!: (value: string) => void;
    qr.dataUrl.and.returnValue(new Promise<string>((done) => { resolve = done; }));
    component.openPix(pending);
    component.closePix();
    resolve('old-image');
    flushMicrotasks();
    expect(component.qrUrl()).toBe('');
    expect(component.pix()).toBeNull();
  }));

  it('cancela a requisição PIX ao fechar', () => {
    const response = new Subject<PixResponse>();
    service.getPix.and.returnValue(response);
    component.openPix(pending);
    component.closePix();
    response.next(pix);
    expect(component.pix()).toBeNull();
  });

  it('mostra erro quando academia não configurou PIX', () => {
    service.getPix.and.returnValue(throwError(() => ({ error: { error: 'PIX não configurado' } })));
    component.openPix(pending);
    expect(component.pixError()).toBe('PIX não configurado');
    expect(component.pixLoading()).toBeFalse();
    component.closePix();
  });

  it('envia uma única notificação e aguarda resposta antes de mostrar sucesso', () => {
    const response = new Subject<void>();
    service.markAwaiting.and.returnValue(response);
    component.markAwaiting(pending);
    component.markAwaiting(pending);
    expect(service.markAwaiting).toHaveBeenCalledTimes(1);
    expect(component.success()).toBe('');
    service.listMine.and.returnValue(of([{ ...pending, status: 'aguardando_confirmacao' }]));
    response.next();
    expect(component.charges()[0].status).toBe('aguardando_confirmacao');
    expect(component.success()).toContain('Aguarde');
    expect(component.busyId()).toBeNull();
  });

  it('não informa pagamento quando usuário cancela', () => {
    (window.confirm as jasmine.Spy).and.returnValue(false);
    component.markAwaiting(pending);
    expect(service.markAwaiting).not.toHaveBeenCalled();
  });

  it('não muda status se API recusar a confirmação', () => {
    service.markAwaiting.and.returnValue(throwError(() => ({ error: { error: 'Cobrança alterada' } })));
    component.markAwaiting(pending);
    expect(component.charges()[0].status).toBe('pending');
    expect(component.error()).toBe('Cobrança alterada');
    expect(component.success()).toBe('');
  });
});