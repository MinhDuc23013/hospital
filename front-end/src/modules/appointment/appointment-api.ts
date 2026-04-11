import api from '../../services/api';
import { ENDPOINTS } from '../../services/endpoints';
import type {
  Appointment,
  AppointmentListResponse,
  BookAppointmentPayload,
  ScheduleAppointmentPayload,
} from '../../shared/types/appointment';

export const appointmentApi = {
  list: (patientId?: string, doctorId?: string, page = 1, pageSize = 20) =>
    api
      .get<AppointmentListResponse>(ENDPOINTS.APPOINTMENTS, {
        params: { patientId, doctorId, page, pageSize },
      })
      .then(r => r.data),

  getById: (id: string) =>
    api.get<Appointment>(`${ENDPOINTS.APPOINTMENTS}/${id}`).then(r => r.data),

  book: (payload: BookAppointmentPayload) =>
    api.post<Appointment>(`${ENDPOINTS.APPOINTMENTS}/book`, payload).then(r => r.data),

  schedule: (payload: ScheduleAppointmentPayload) =>
    api.post<Appointment>(ENDPOINTS.APPOINTMENTS, payload).then(r => r.data),

  cancel: (id: string) =>
    api.delete(`${ENDPOINTS.APPOINTMENTS}/${id}`).then(r => r.data),

  complete: (id: string) =>
    api.post<Appointment>(`${ENDPOINTS.APPOINTMENTS}/${id}/complete`).then(r => r.data),
};
