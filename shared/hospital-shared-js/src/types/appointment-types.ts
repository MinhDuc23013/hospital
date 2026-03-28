export type AppointmentStatus = 'Scheduled' | 'Completed' | 'Cancelled' | 'NoShow';

/** Appointment data transferred between services */
export interface AppointmentDto {
  id: string;
  patientId: string;
  providerId: string;
  scheduledTime: string; // ISO datetime
  durationMinutes: number;
  status: AppointmentStatus;
  notes?: string;
  createdAt: string;
}
