import api from '../../services/api';
import { ENDPOINTS } from '../../services/endpoints';
import type {
  MedicalRecord,
  MedicalRecordListResponse,
  CreateMedicalRecordPayload,
  UpdateMedicalRecordPayload,
} from '../../shared/types/medical-record';

export const medicalRecordApi = {
  getByPatientId: (patientId: string, page = 1, pageSize = 20) =>
    api
      .get<MedicalRecordListResponse>(`${ENDPOINTS.MEDICAL_RECORDS}/patient/${patientId}`, {
        params: { page, pageSize },
      })
      .then(r => r.data),

  getById: (id: string) =>
    api.get<MedicalRecord>(`${ENDPOINTS.MEDICAL_RECORDS}/${id}`).then(r => r.data),

  create: (payload: CreateMedicalRecordPayload) =>
    api.post<MedicalRecord>(ENDPOINTS.MEDICAL_RECORDS, payload).then(r => r.data),

  update: (id: string, payload: UpdateMedicalRecordPayload) =>
    api.put<MedicalRecord>(`${ENDPOINTS.MEDICAL_RECORDS}/${id}`, payload).then(r => r.data),
};
