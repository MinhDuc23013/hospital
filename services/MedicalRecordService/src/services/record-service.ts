import { createLogger } from '@hospital/shared';
import { MedicalRecord, MedicalRecordDocument } from '../models/medical-record';

interface CreateRecordData {
  patientId: string;
  appointmentId: string;
  findings?: string;
  diagnosis?: string[];
  createdBy?: string;
}

interface PaginatedResult<T> {
  items: T[];
  total: number;
  page: number;
  pageSize: number;
}

const logger = createLogger('medical-record-service');

export class RecordService {
  async create(data: CreateRecordData): Promise<MedicalRecordDocument> {
    const record = new MedicalRecord(data);
    await record.save();
    logger.info('Medical record created', { recordId: record._id, patientId: data.patientId });
    return record;
  }

  async getByPatientId(patientId: string, page = 1, pageSize = 50): Promise<PaginatedResult<MedicalRecordDocument>> {
    const skip = (page - 1) * pageSize;
    const [items, total] = await Promise.all([
      MedicalRecord.find({ patientId }).skip(skip).limit(pageSize).sort({ createdAt: -1 }),
      MedicalRecord.countDocuments({ patientId }),
    ]);
    return { items, total, page, pageSize };
  }

  async getById(id: string): Promise<MedicalRecordDocument | null> {
    return MedicalRecord.findById(id);
  }

  async update(id: string, data: Partial<CreateRecordData>): Promise<MedicalRecordDocument | null> {
    return MedicalRecord.findByIdAndUpdate(id, data, { new: true });
  }
}
