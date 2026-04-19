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
  scheduleId: string;
  slotId: string;
  scheduledTime: string;
  durationMinutes: number;
  paymentAmount: number;
  paymentMethod: 'Cash' | 'Card' | 'Insurance' | 'BankTransfer';
  currency?: string;
  notes?: string;
}

export interface DoctorScheduleListItem {
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
}

export interface TimeSlot {
  id: string;
  scheduleId: string;
  startTime: string;
  endTime: string;
  status: 'Available' | 'Reserved' | 'Confirmed' | number;
  patientId?: string;
  appointmentId?: string;
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
