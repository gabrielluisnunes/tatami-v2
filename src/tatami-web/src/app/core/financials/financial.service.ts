import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../environments/environment';
import { FinancialOverview, PixResponse, StudentFinancial } from './financial.models';

@Injectable({ providedIn: 'root' })
export class FinancialService {
  private readonly adminUrl = `${environment.apiUrl}/api/financials`;
  private readonly studentUrl = `${environment.apiUrl}/api/students/me/financials`;

  constructor(private readonly http: HttpClient) {}

  overview(month: string) {
    return this.http.get<FinancialOverview>(this.adminUrl, { params: { month } });
  }

  markPaid(financialId: string) {
    return this.http.post<void>(`${this.adminUrl}/mark-paid`, { financialId });
  }

  manualPayment(request: { studentId: string; amount: number; paidAt: string }) {
    return this.http.post<void>(`${this.adminUrl}/manual-payment`, request);
  }

  exportOverdue(month: string) {
    return this.http.get(`${this.adminUrl}/export-overdue`, { params: { month }, responseType: 'blob' });
  }

  listMine() {
    return this.http.get<StudentFinancial[]>(this.studentUrl);
  }

  getPix(id: string) {
    return this.http.get<PixResponse>(`${this.studentUrl}/${id}/pix`);
  }

  markAwaiting(id: string) {
    return this.http.post<void>(`${this.studentUrl}/${id}/aguardando`, {});
  }
}