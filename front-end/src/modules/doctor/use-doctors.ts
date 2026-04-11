import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { doctorApi } from './doctor-api';
import type { CreateDoctorPayload, UpdateDoctorPayload, CreateSchedulePayload } from '../../shared/types/doctor';

const DOCTORS_KEY = 'doctors';
const SCHEDULES_KEY = 'doctor-schedules';

export function useDoctors(specialty?: string, isActive?: boolean, page = 1, pageSize = 20) {
  return useQuery({
    queryKey: [DOCTORS_KEY, specialty, isActive, page, pageSize],
    queryFn: () => doctorApi.list(specialty, isActive, page, pageSize),
  });
}

export function useDoctor(id: string) {
  return useQuery({
    queryKey: [DOCTORS_KEY, id],
    queryFn: () => doctorApi.getById(id),
    enabled: !!id,
  });
}

export function useCreateDoctor() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (payload: CreateDoctorPayload) => doctorApi.create(payload),
    onSuccess: () => qc.invalidateQueries({ queryKey: [DOCTORS_KEY] }),
  });
}

export function useUpdateDoctor(id: string) {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (payload: UpdateDoctorPayload) => doctorApi.update(id, payload),
    onSuccess: () => qc.invalidateQueries({ queryKey: [DOCTORS_KEY] }),
  });
}

export function useDeactivateDoctor() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (id: string) => doctorApi.deactivate(id),
    onSuccess: () => qc.invalidateQueries({ queryKey: [DOCTORS_KEY] }),
  });
}

export function useDoctorSchedules(doctorId?: string, date?: string, page = 1, pageSize = 20) {
  return useQuery({
    queryKey: [SCHEDULES_KEY, doctorId, date, page, pageSize],
    queryFn: () => doctorApi.listSchedules(doctorId, date, page, pageSize),
  });
}

export function useCreateSchedule() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (payload: CreateSchedulePayload) => doctorApi.createSchedule(payload),
    onSuccess: () => qc.invalidateQueries({ queryKey: [SCHEDULES_KEY] }),
  });
}
