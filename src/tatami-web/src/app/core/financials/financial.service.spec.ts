import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { environment } from '../../../environments/environment';
import { FinancialService } from './financial.service';
import { currentFinancialMonth } from './financial.models';
import { PixQrService } from './pix-qr.service';

describe('FinancialService', () => {
  let service: FinancialService;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({ providers: [provideHttpClient(), provideHttpClientTesting()] });
    service = TestBed.inject(FinancialService);
    http = TestBed.inject(HttpTestingController);
  });
  afterEach(() => http.verify());

  it('usa a competência selecionada no overview e export', () => {
    service.overview('2026-07').subscribe();
    const overview = http.expectOne(`${environment.apiUrl}/api/financials?month=2026-07`);
    expect(overview.request.method).toBe('GET');
    overview.flush({});
    service.exportOverdue('2026-07').subscribe();
    const exportRequest = http.expectOne(`${environment.apiUrl}/api/financials/export-overdue?month=2026-07`);
    expect(exportRequest.request.responseType).toBe('blob');
    exportRequest.flush(new Blob());
  });

  it('envia confirmação e pagamento manual sem IDs de academia fornecidos pelo browser', () => {
    service.markPaid('charge').subscribe();
    const paid = http.expectOne(`${environment.apiUrl}/api/financials/mark-paid`);
    expect(paid.request.body).toEqual({ financialId: 'charge' });
    paid.flush(null);
    const request = { studentId: 'student', amount: 150, paidAt: '2026-09-10T12:00:00Z' };
    service.manualPayment(request).subscribe();
    const manual = http.expectOne(`${environment.apiUrl}/api/financials/manual-payment`);
    expect(manual.request.body).toEqual(request);
    manual.flush(null);
  });

  it('usa as rotas autenticadas me para listar, gerar PIX e informar pagamento', () => {
    const url = `${environment.apiUrl}/api/students/me/financials`;
    service.listMine().subscribe();
    http.expectOne(url).flush([]);
    service.getPix('charge').subscribe();
    http.expectOne(`${url}/charge/pix`).flush({});
    service.markAwaiting('charge').subscribe();
    const awaiting = http.expectOne(`${url}/charge/aguardando`);
    expect(awaiting.request.method).toBe('POST');
    expect(awaiting.request.body).toEqual({});
    awaiting.flush(null);
  });

  it('seleciona o mês de Brasília na virada UTC', () => {
    expect(currentFinancialMonth(new Date('2026-10-01T02:59:59Z'))).toBe('2026-09');
    expect(currentFinancialMonth(new Date('2026-10-01T03:00:00Z'))).toBe('2026-10');
  });

  it('gera imagem QR com a biblioteca real', async () => {
    const image = await TestBed.inject(PixQrService).dataUrl('00020126330014BR.GOV.BCB.PIX0111example-key6304ABCD');
    expect(image).toMatch(/^data:image\/png;base64,/);
  });
});