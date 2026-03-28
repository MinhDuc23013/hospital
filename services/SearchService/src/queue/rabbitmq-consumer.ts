import { RabbitMQClient, EventNames, createLogger } from '@hospital/shared';
import { IndexerService } from '../services/indexer-service';

const logger = createLogger('search-service');

/** Normalize .NET PascalCase raw JSON to camelCase PatientEvent fields */
function normalizePatientEvent(raw: unknown): { patientId: string; firstName: string; lastName: string; email: string } {
  const e = raw as Record<string, string>;
  return {
    patientId: e['patientId'] ?? e['PatientId'] ?? '',
    firstName: e['firstName'] ?? e['FirstName'] ?? '',
    lastName:  e['lastName']  ?? e['LastName']  ?? '',
    email:     e['email']     ?? e['Email']      ?? '',
  };
}

export async function startConsumers(
  indexerService: IndexerService,
  rabbitmqUrl: string,
): Promise<void> {
  const client = new RabbitMQClient(rabbitmqUrl);
  try {
    await client.connect();

    await client.subscribe(
      'hospital.events',
      EventNames.PatientCreated,
      'search.patient-created',
      async (msg: unknown) => {
        logger.info('[STEP 3] PatientCreated event received from RabbitMQ', { msg });
        // Normalize camelCase/PascalCase — .NET raw JSON serializes as PascalCase
        const event = normalizePatientEvent(msg);
        try {
          await indexerService.indexPatient({
            patientId: event.patientId,
            firstName: event.firstName,
            lastName: event.lastName,
            email: event.email,
          });
          logger.info('[STEP 4] Patient indexed to Elasticsearch', { patientId: event.patientId });
        } catch (err) {
          logger.error('[STEP 4 FAILED] Failed to index patient to Elasticsearch', { patientId: event.patientId, error: err });
        }
      },
    );

    await client.subscribe(
      'hospital.events',
      EventNames.PatientUpdated,
      'search.patient-updated',
      async (msg: unknown) => {
        logger.info('[STEP 3] PatientUpdated event received from RabbitMQ', { msg });
        // PatientUpdated shares same shape as PatientCreated for indexing purposes
        const event = normalizePatientEvent(msg);
        try {
          await indexerService.indexPatient({
            patientId: event.patientId,
            firstName: event.firstName,
            lastName: event.lastName,
            email: event.email,
          });
          logger.info('[STEP 4] Patient re-indexed to Elasticsearch', { patientId: event.patientId });
        } catch (err) {
          logger.error('[STEP 4 FAILED] Failed to re-index patient to Elasticsearch', { patientId: event.patientId, error: err });
        }
      },
    );

    logger.info('RabbitMQ consumers started');
  } catch (err) {
    logger.warn('RabbitMQ not available at startup', { error: err });
  }
}
