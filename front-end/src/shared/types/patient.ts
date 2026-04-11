export interface Patient {
  id: string;
  keycloakUserId?: string;
  email: string;
  firstName: string;
  lastName: string;
  dateOfBirth: string; // ISO date string "YYYY-MM-DD"
  phoneNumber?: string;
  createdAt: string;
  updatedAt?: string;
}

export interface PatientListResponse {
  data: Patient[];
  pagination: { total: number; page: number; pageSize: number };
}

export interface CreatePatientPayload {
  email: string;
  firstName: string;
  lastName: string;
  dateOfBirth: string;
  phoneNumber?: string;
  password: string;
}

export interface UpdatePatientPayload {
  firstName: string;
  lastName: string;
  phoneNumber?: string;
}
