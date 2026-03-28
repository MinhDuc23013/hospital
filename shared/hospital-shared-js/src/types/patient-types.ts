/** Patient profile data transferred between services */
export interface PatientDto {
  id: string;
  email: string;
  firstName: string;
  lastName: string;
  dateOfBirth: string; // ISO date string
  phoneNumber?: string;
  createdAt: string; // ISO datetime
  updatedAt?: string;
}

/** Minimal patient reference used in related entities */
export interface PatientRef {
  id: string;
  fullName: string;
  email: string;
}
