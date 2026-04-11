import api from '../../services/api';
import { ENDPOINTS } from '../../services/endpoints';
import type { Payment, PaymentListResponse } from '../../shared/types/payment';

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
};
