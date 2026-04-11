import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { patientApi } from './patient-api';
import type { CreatePatientPayload, UpdatePatientPayload } from '../../shared/types/patient';

const QUERY_KEY = 'patients';

export function usePatients(page = 1, pageSize = 20) {
  return useQuery({
    queryKey: [QUERY_KEY, page, pageSize],
    queryFn: () => patientApi.list(page, pageSize),
  });
}

export function usePatient(id: string) {
  return useQuery({
    queryKey: [QUERY_KEY, id],
    queryFn: () => patientApi.getById(id),
    enabled: !!id,
  });
}

export function useCreatePatient() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (payload: CreatePatientPayload) => patientApi.create(payload),
    onSuccess: () => qc.invalidateQueries({ queryKey: [QUERY_KEY] }),
  });
}

export function useUpdatePatient(id: string) {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (payload: UpdatePatientPayload) => patientApi.update(id, payload),
    onSuccess: () => qc.invalidateQueries({ queryKey: [QUERY_KEY] }),
  });
}

export function useDeletePatient() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (id: string) => patientApi.delete(id),
    onSuccess: () => qc.invalidateQueries({ queryKey: [QUERY_KEY] }),
  });
}
