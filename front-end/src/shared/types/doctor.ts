export interface Doctor {
  id: string;
  keycloakUserId?: string;
  fullName: string;
  specialty: string;
  phone?: string;
  email?: string;
  isActive: boolean;
  createdAt: string;
}

export interface DoctorSchedule {
  id: string;
  doctorId: string;
  doctorName: string;
  date: string;
  startTime: string;
  endTime: string;
  slotDurationMinutes: number;
  totalSlots: number;
  availableSlots: number;
  status: string;
  createdAt: string;
}

export interface TimeSlot {
  id: string;
  scheduleId: string;
  startTime: string;
  endTime: string;
  status: string;
  appointmentId?: string;
  patientId?: string;
  reservedUntil?: string;
}

export interface CreateDoctorPayload {
  fullName: string;
  specialty: string;
  phone?: string;
  email?: string;
  password: string;
}

export interface UpdateDoctorPayload {
  fullName?: string;
  specialty?: string;
  phone?: string;
  email?: string;
}

export interface CreateSchedulePayload {
  doctorId: string;
  date: string;
  startTime: string;
  endTime: string;
  slotDurationMinutes: number;
}

export interface DoctorListResponse {
  data: Doctor[];
  pagination: { total: number; page: number; pageSize: number };
}

export interface ScheduleListResponse {
  data: DoctorSchedule[];
  pagination: { total: number; page: number; pageSize: number };
}
