import api from '../../services/api';
import { ENDPOINTS } from '../../services/endpoints';
import type {
  Doctor,
  DoctorListResponse,
  DoctorSchedule,
  ScheduleListResponse,
  TimeSlot,
  CreateDoctorPayload,
  UpdateDoctorPayload,
  CreateSchedulePayload,
} from '../../shared/types/doctor';

export const doctorApi = {
  list: (specialty?: string, isActive?: boolean, page = 1, pageSize = 20) =>
    api
      .get<DoctorListResponse>(ENDPOINTS.DOCTORS, {
        params: { specialty, isActive, page, pageSize },
      })
      .then(r => r.data),

  getById: (id: string) =>
    api.get<Doctor>(`${ENDPOINTS.DOCTORS}/${id}`).then(r => r.data),

  create: (payload: CreateDoctorPayload) =>
    api.post<Doctor>(ENDPOINTS.DOCTORS, payload).then(r => r.data),

  update: (id: string, payload: UpdateDoctorPayload) =>
    api.put<Doctor>(`${ENDPOINTS.DOCTORS}/${id}`, payload).then(r => r.data),

  deactivate: (id: string) =>
    api.delete(`${ENDPOINTS.DOCTORS}/${id}`).then(r => r.data),

  listSchedules: (doctorId?: string, date?: string, page = 1, pageSize = 20) =>
    api
      .get<ScheduleListResponse>(ENDPOINTS.DOCTOR_SCHEDULES, {
        params: { doctorId, date, page, pageSize },
      })
      .then(r => r.data),

  createSchedule: (payload: CreateSchedulePayload) =>
    api.post<DoctorSchedule>(ENDPOINTS.DOCTOR_SCHEDULES, payload).then(r => r.data),

  getAvailableSlots: (scheduleId: string) =>
    api
      .get<TimeSlot[]>(`${ENDPOINTS.DOCTOR_SCHEDULES}/${scheduleId}/slots/available`)
      .then(r => r.data),
};
