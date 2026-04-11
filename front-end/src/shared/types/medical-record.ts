export interface LabResult {
  testName: string;
  result?: string;
  normalRange?: string;
  timestamp: string;
}

export interface MedicalRecord {
  id: string;
  patientId: string;
  appointmentId: string;
  findings: string;
  diagnosis: string[];
  labResults: LabResult[];
  createdBy?: string;
  createdAt: string;
  updatedAt: string;
}

export interface CreateMedicalRecordPayload {
  patientId: string;
  appointmentId: string;
  findings?: string;
  diagnosis?: string[];
  createdBy?: string;
}

export interface UpdateMedicalRecordPayload {
  findings?: string;
  diagnosis?: string[];
  labResults?: { testName: string; result?: string; normalRange?: string }[];
}

export interface MedicalRecordListResponse {
  data: MedicalRecord[];
  pagination: { total: number; page: number; pageSize: number };
}
