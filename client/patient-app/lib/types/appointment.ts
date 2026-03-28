// Appointment entity — mirrors Appointment Service PostgreSQL schema.

export type AppointmentStatus = 'Scheduled' | 'InProgress' | 'Completed' | 'Cancelled';

export interface Appointment {
  id: string;
  patientId: string;
  providerId: string;
  /** ISO 8601 datetime string */
  scheduledTime: string;
  /** ISO 8601 duration string, e.g. "PT30M" */
  duration: string;
  status: AppointmentStatus;
  notes?: string;
  createdAt: string;
  updatedAt: string;
}
