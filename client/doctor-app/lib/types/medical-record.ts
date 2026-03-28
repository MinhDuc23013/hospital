// Medical record entity — mirrors Medical Record Service MongoDB schema.

export interface LabResult {
  testName: string;
  result: string;
  normalRange: string;
  timestamp: string;
}

export interface RecordDocument {
  filename: string;
  s3Url: string;
  uploadedAt: string;
}

export interface MedicalRecord {
  /** MongoDB ObjectId as string */
  _id: string;
  patientId: string;
  appointmentId: string;
  findings: string;
  diagnosis: string[];
  labResults: LabResult[];
  documents: RecordDocument[];
  /** Doctor ID who created the record */
  createdBy: string;
  createdAt: string;
  updatedAt: string;
}
