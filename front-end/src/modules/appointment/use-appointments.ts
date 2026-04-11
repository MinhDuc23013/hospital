import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { appointmentApi } from './appointment-api';
import type { BookAppointmentPayload, ScheduleAppointmentPayload } from '../../shared/types/appointment';

const QUERY_KEY = 'appointments';

export function useAppointments(patientId?: string, doctorId?: string, page = 1, pageSize = 20) {
  return useQuery({
    queryKey: [QUERY_KEY, patientId, doctorId, page, pageSize],
    queryFn: () => appointmentApi.list(patientId, doctorId, page, pageSize),
  });
}

export function useBookAppointment() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (payload: BookAppointmentPayload) => appointmentApi.book(payload),
    onSuccess: () => qc.invalidateQueries({ queryKey: [QUERY_KEY] }),
  });
}

export function useScheduleAppointment() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (payload: ScheduleAppointmentPayload) => appointmentApi.schedule(payload),
    onSuccess: () => qc.invalidateQueries({ queryKey: [QUERY_KEY] }),
  });
}

export function useCancelAppointment() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (id: string) => appointmentApi.cancel(id),
    onSuccess: () => qc.invalidateQueries({ queryKey: [QUERY_KEY] }),
  });
}

export function useCompleteAppointment() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (id: string) => appointmentApi.complete(id),
    onSuccess: () => qc.invalidateQueries({ queryKey: [QUERY_KEY] }),
  });
}
