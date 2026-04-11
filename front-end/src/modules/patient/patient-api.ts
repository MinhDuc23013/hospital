import api from '../../services/api';
import { ENDPOINTS } from '../../services/endpoints';
import type {
  Patient,
  PatientListResponse,
  CreatePatientPayload,
  UpdatePatientPayload,
} from '../../shared/types/patient';

export const patientApi = {
  list: (page = 1, pageSize = 20) =>
    api
      .get<PatientListResponse>(ENDPOINTS.PATIENTS, { params: { page, pageSize } })
      .then(r => r.data),

  getById: (id: string) =>
    api.get<Patient>(`${ENDPOINTS.PATIENTS}/${id}`).then(r => r.data),

  create: (payload: CreatePatientPayload) =>
    api.post<Patient>(ENDPOINTS.PATIENTS, payload).then(r => r.data),

  update: (id: string, payload: UpdatePatientPayload) =>
    api.put<Patient>(`${ENDPOINTS.PATIENTS}/${id}`, payload).then(r => r.data),

  delete: (id: string) =>
    api.delete(`${ENDPOINTS.PATIENTS}/${id}`).then(r => r.data),
};
