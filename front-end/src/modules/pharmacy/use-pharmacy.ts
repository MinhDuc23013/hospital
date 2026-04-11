import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { pharmacyApi } from './pharmacy-api';
import type { CreateDrugPayload, UpdateDrugPayload } from '../../shared/types/pharmacy';

const DRUGS_KEY = 'drugs';
const PRESCRIPTIONS_KEY = 'prescriptions';

export function useDrugs(name?: string, lowStock?: boolean, page = 1, pageSize = 20) {
  return useQuery({
    queryKey: [DRUGS_KEY, name, lowStock, page, pageSize],
    queryFn: () => pharmacyApi.listDrugs(name, lowStock, page, pageSize),
  });
}

export function useCreateDrug() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (payload: CreateDrugPayload) => pharmacyApi.createDrug(payload),
    onSuccess: () => qc.invalidateQueries({ queryKey: [DRUGS_KEY] }),
  });
}

export function useUpdateDrug(id: string) {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (payload: UpdateDrugPayload) => pharmacyApi.updateDrug(id, payload),
    onSuccess: () => qc.invalidateQueries({ queryKey: [DRUGS_KEY] }),
  });
}

export function useDispensePrescription() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (id: string) => pharmacyApi.dispensePrescription(id),
    onSuccess: () => qc.invalidateQueries({ queryKey: [PRESCRIPTIONS_KEY] }),
  });
}

export function usePrescriptionLookup(id: string) {
  return useQuery({
    queryKey: [PRESCRIPTIONS_KEY, id],
    queryFn: () => pharmacyApi.getPrescription(id),
    enabled: !!id,
    retry: false,
  });
}
