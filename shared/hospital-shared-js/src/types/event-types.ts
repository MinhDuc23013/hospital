/** Base structure for all domain events */
export interface DomainEvent {
  timestamp: string; // ISO datetime
}

export interface PatientCreatedEvent extends DomainEvent {
  patientId: string;
  email: string;
  firstName: string;
  lastName: string;
}

export interface AppointmentScheduledEvent extends DomainEvent {
  appointmentId: string;
  patientId: string;
  providerId: string;
  scheduledTime: string;
  durationMinutes: number;
}

export interface PrescriptionIssuedEvent extends DomainEvent {
  prescriptionId: string;
  patientId: string;
  drugId: string;
  quantity: number;
}

export interface InventoryLowEvent extends DomainEvent {
  drugId: string;
  drugName: string;
  currentStock: number;
  minimumStock: number;
}
