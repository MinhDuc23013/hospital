import mongoose, { Document, Schema } from 'mongoose';

interface LabResult {
  testName: string;
  result?: string;
  normalRange?: string;
  timestamp: Date;
}

interface MedicalRecordDocument extends Document {
  patientId: string;
  appointmentId: string;
  findings: string;
  diagnosis: string[];
  labResults: LabResult[];
  createdBy?: string;
  createdAt: Date;
  updatedAt: Date;
}

const labResultSchema = new Schema<LabResult>({
  testName: { type: String, required: true },
  result: String,
  normalRange: String,
  timestamp: { type: Date, default: Date.now },
});

const medicalRecordSchema = new Schema<MedicalRecordDocument>({
  patientId: { type: String, required: true, index: true },
  appointmentId: { type: String, required: true },
  findings: { type: String, default: '' },
  diagnosis: { type: [String], default: [] },
  labResults: { type: [labResultSchema], default: [] },
  createdBy: String,
}, { timestamps: true });

export const MedicalRecord = mongoose.model<MedicalRecordDocument>('MedicalRecord', medicalRecordSchema);
export type { MedicalRecordDocument };
