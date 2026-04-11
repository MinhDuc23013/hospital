import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { paymentApi } from './payment-api';

const QUERY_KEY = 'payments';

export function usePayments(appointmentId?: string, patientId?: string, status?: string, page = 1, pageSize = 20) {
  return useQuery({
    queryKey: [QUERY_KEY, appointmentId, patientId, status, page, pageSize],
    queryFn: () => paymentApi.list(appointmentId, patientId, status, page, pageSize),
  });
}

export function useProcessPayment() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (id: string) => paymentApi.process(id),
    onSuccess: () => qc.invalidateQueries({ queryKey: [QUERY_KEY] }),
  });
}

export function useRefundPayment() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (id: string) => paymentApi.refund(id),
    onSuccess: () => qc.invalidateQueries({ queryKey: [QUERY_KEY] }),
  });
}
