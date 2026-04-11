import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { medicalRecordApi } from './medical-record-api';
import type { CreateMedicalRecordPayload, UpdateMedicalRecordPayload } from '../../shared/types/medical-record';

const QUERY_KEY = 'medical-records';

export function useMedicalRecordsByPatient(patientId: string, page = 1, pageSize = 20) {
  return useQuery({
    queryKey: [QUERY_KEY, 'patient', patientId, page, pageSize],
    queryFn: () => medicalRecordApi.getByPatientId(patientId, page, pageSize),
    enabled: !!patientId,
  });
}

export function useCreateMedicalRecord() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (payload: CreateMedicalRecordPayload) => medicalRecordApi.create(payload),
    onSuccess: () => qc.invalidateQueries({ queryKey: [QUERY_KEY] }),
  });
}

export function useUpdateMedicalRecord(id: string) {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (payload: UpdateMedicalRecordPayload) => medicalRecordApi.update(id, payload),
    onSuccess: () => qc.invalidateQueries({ queryKey: [QUERY_KEY] }),
  });
}
