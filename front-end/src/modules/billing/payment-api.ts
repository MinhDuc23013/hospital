import api from '../../services/api';
import { ENDPOINTS } from '../../services/endpoints';
import type { Payment, PaymentListResponse, CashSession } from '../../shared/types/payment';

const CASH_SESSIONS = '/api/cash-sessions';

export const paymentApi = {
  list: (appointmentId?: string, patientId?: string, status?: string, page = 1, pageSize = 20) =>
    api
      .get<PaymentListResponse>(ENDPOINTS.PAYMENTS, {
        params: { appointmentId, patientId, status, page, pageSize },
      })
      .then(r => r.data),

  getById: (id: string) =>
    api.get<Payment>(`${ENDPOINTS.PAYMENTS}/${id}`).then(r => r.data),

  process: (id: string) =>
    api.post<Payment>(`${ENDPOINTS.PAYMENTS}/${id}/process`).then(r => r.data),

  refund: (id: string) =>
    api.post<Payment>(`${ENDPOINTS.PAYMENTS}/${id}/refund`).then(r => r.data),

  // Cash-specific endpoints
  completeCash: (paymentId: string, body: { amountReceived: number; cashierId: string; cashSessionId: string }) =>
    api.post<Payment>(`${ENDPOINTS.PAYMENTS}/${paymentId}/complete-cash`, body).then(r => r.data),

  openCashSession: (body: { cashierId: string; cashierName: string; counterId: string; openingBalance: number }) =>
    api.post<CashSession>(`${CASH_SESSIONS}/open`, body).then(r => r.data),

  closeCashSession: (sessionId: string, body: { actualCash: number; notes?: string }) =>
    api.post<CashSession>(`${CASH_SESSIONS}/${sessionId}/close`, body).then(r => r.data),

  getCurrentCashSession: (cashierId: string) =>
    api.get<CashSession>(`${CASH_SESSIONS}/current/${cashierId}`).then(r => r.data).catch(() => null),
};
