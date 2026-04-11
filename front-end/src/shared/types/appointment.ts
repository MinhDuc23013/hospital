export interface Appointment {
  id: string;
  patientId: string;
  doctorId: string;
  scheduledTime: string;
  durationMinutes: number;
  status: 'Scheduled' | 'Completed' | 'Cancelled';
  notes?: string;
  createdAt: string;
}

export interface BookAppointmentPayload {
  patientId: string;
  doctorId: string;
  scheduledTime: string;
  durationMinutes: number;
  notes?: string;
  paymentMethod: 'Cash' | 'Card' | 'Insurance';
}

export interface ScheduleAppointmentPayload {
  patientId: string;
  doctorId: string;
  scheduledTime: string;
  durationMinutes: number;
  notes?: string;
}

export interface AppointmentListResponse {
  data: Appointment[];
  pagination: { total: number; page: number; pageSize: number };
}
