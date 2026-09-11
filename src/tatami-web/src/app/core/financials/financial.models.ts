export type FinancialStatus = 'pending' | 'paid' | 'overdue' | 'aguardando_confirmacao';

export interface FinancialRow {
  studentId: string;
  studentName: string;
  email: string;
  isActive: boolean;
  paymentDueDay: number | null;
  financialId: string | null;
  amount: number | null;
  dueDate: string | null;
  referenceMonth: string;
  status: FinancialStatus | 'sem_cobranca';
  paidAt: string | null;
}

export interface FinancialOverview {
  month: string;
  received: number;
  overdueAmount: number;
  overdueCount: number;
  chargeCount: number;
  awaitingCount: number;
  students: FinancialRow[];
}

export interface StudentFinancial {
  id: string;
  amount: number;
  dueDate: string;
  referenceMonth: string;
  status: FinancialStatus;
  paidAt: string | null;
}

export interface PixResponse {
  financialId: string;
  amount: number;
  payload: string;
}

export const financialStatusLabels: Record<FinancialStatus | 'sem_cobranca', string> = {
  pending: 'Pendente',
  paid: 'Pago',
  overdue: 'Em atraso',
  aguardando_confirmacao: 'Aguardando confirmação',
  sem_cobranca: 'Sem cobrança',
};

export function currentFinancialMonth(now = new Date()): string {
  return new Date(now.getTime() - 3 * 60 * 60 * 1000).toISOString().slice(0, 7);
}