import { Prescription } from '../models/prescription';
import { Drug } from '../models/drug';
import { EventNames, createLogger } from '@hospital/shared';
import { KafkaPublisher } from './kafka-publisher';

const logger = createLogger('pharmacy-service');

interface CreatePrescriptionData {
  patientId: string;
  drugId: number;
  quantity: number;
  instructions?: string;
  validUntil?: Date;
}

export class PrescriptionService {
  constructor(private readonly kafkaPublisher?: KafkaPublisher) {}

  async create(data: CreatePrescriptionData): Promise<Prescription> {
    const prescription = await Prescription.create(data);

    // Kafka — audit/trace event
    if (this.kafkaPublisher) {
      await this.kafkaPublisher.publish(EventNames.PrescriptionIssued, {
        prescriptionId: prescription.id,
        patientId: prescription.patientId,
        drugId: String(prescription.drugId),
        quantity: prescription.quantity,
        timestamp: new Date().toISOString(),
      }).catch(err => logger.warn('Failed to publish PrescriptionIssuedEvent to Kafka', { error: err }));
    }

    return prescription;
  }

  async getById(id: string): Promise<Prescription | null> {
    return Prescription.findByPk(id);
  }

  async dispense(id: string): Promise<Prescription | null> {
    const prescription = await Prescription.findByPk(id);
    if (!prescription) return null;

    await prescription.update({ status: 'Dispensed' });

    // Decrement drug stock
    await Drug.decrement('currentStock', { by: prescription.quantity, where: { id: prescription.drugId } });
    logger.info('Prescription dispensed', { prescriptionId: id });

    return prescription;
  }
}
