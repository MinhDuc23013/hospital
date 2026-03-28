import { RabbitMQClient, EventNames, AppointmentScheduledEvent, createLogger } from '@hospital/shared';
import { RecordService } from '../services/record-service';

const logger = createLogger('medical-record-service');

export async function startConsumers(recordService: RecordService, rabbitmqUrl: string): Promise<void> {
  const client = new RabbitMQClient(rabbitmqUrl);
  try {
    await client.connect();

    // Auto-create empty medical record when appointment is scheduled
    await client.subscribe(
      'hospital.events',
      EventNames.AppointmentScheduled,
      'medical-record.appointment-scheduled',
      async (msg: unknown) => {
        const event = msg as AppointmentScheduledEvent;
        await recordService.create({
          patientId: event.patientId,
          appointmentId: event.appointmentId,
          findings: '',
          diagnosis: [],
        });
        logger.info('Empty medical record created for appointment', { appointmentId: event.appointmentId });
      }
    );

    logger.info('RabbitMQ consumers started');
  } catch (err) {
    logger.warn('RabbitMQ not available at startup, consumers not started', { error: err });
  }
}
