// Prescription entity — mirrors Pharmacy Service SQL Server schema.

export type PrescriptionStatus = 'Pending' | 'Dispensed' | 'Expired';

export interface Prescription {
  id: string;
  patientId: string;
  drugId: number;
  quantity: number;
  instructions: string;
  issuedAt: string;
  validUntil: string;
  status: PrescriptionStatus;
}
