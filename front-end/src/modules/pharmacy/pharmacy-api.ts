import api from '../../services/api';
import { ENDPOINTS } from '../../services/endpoints';
import type {
  Drug,
  DrugListResponse,
  Prescription,
  CreateDrugPayload,
  UpdateDrugPayload,
} from '../../shared/types/pharmacy';

export const pharmacyApi = {
  listDrugs: (name?: string, lowStock?: boolean, page = 1, pageSize = 20) =>
    api
      .get<DrugListResponse>(ENDPOINTS.DRUGS, {
        params: { name, lowStock, page, pageSize },
      })
      .then(r => r.data),

  createDrug: (payload: CreateDrugPayload) =>
    api.post<Drug>(ENDPOINTS.DRUGS, payload).then(r => r.data),

  updateDrug: (id: string, payload: UpdateDrugPayload) =>
    api.put<Drug>(`${ENDPOINTS.DRUGS}/${id}`, payload).then(r => r.data),

  getPrescription: (id: string) =>
    api.get<Prescription>(`${ENDPOINTS.PRESCRIPTIONS}/${id}`).then(r => r.data),

  dispensePrescription: (id: string) =>
    api.post<Prescription>(`${ENDPOINTS.PRESCRIPTIONS}/${id}/dispense`).then(r => r.data),
};
