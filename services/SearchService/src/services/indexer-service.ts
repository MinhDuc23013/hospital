import { Client } from '@elastic/elasticsearch';
import type { MappingProperty } from '@elastic/elasticsearch/lib/api/types';
import { createLogger } from '@hospital/shared';

const logger = createLogger('search-service');

const PATIENT_INDEX = 'hospital-patients';
const DRUG_INDEX = 'hospital-drugs';

interface PatientDoc {
  patientId: string;
  firstName: string;
  lastName: string;
  email: string;
}

interface DrugDoc {
  id: number;
  name: string;
  code: string;
  dosage?: string;
}

export class IndexerService {
  constructor(private readonly client: Client) {}

  /** Create ES indices if they don't exist — idempotent */
  async ensureIndices(): Promise<void> {
    await this.ensureIndex(PATIENT_INDEX, {
      patientId: { type: 'keyword' },
      firstName: { type: 'text' },
      lastName: { type: 'text' },
      email: { type: 'keyword' },
    });

    await this.ensureIndex(DRUG_INDEX, {
      id: { type: 'integer' },
      name: { type: 'text' },
      code: { type: 'keyword' },
      dosage: { type: 'text' },
    });

    logger.info('Elasticsearch indices ready');
  }

  private async ensureIndex(
    index: string,
    properties: Record<string, MappingProperty>,
  ): Promise<void> {
    const exists = await this.client.indices.exists({ index });
    if (!exists) {
      await this.client.indices.create({
        index,
        mappings: { properties },
        settings: { number_of_shards: 1, number_of_replicas: 0 },
      });
      logger.info(`Created index: ${index}`);
    }
  }

  async indexPatient(patient: PatientDoc): Promise<void> {
    await this.client.index({
      index: PATIENT_INDEX,
      id: patient.patientId,
      document: patient,
    });
  }

  async indexDrug(drug: DrugDoc): Promise<void> {
    await this.client.index({
      index: DRUG_INDEX,
      id: String(drug.id),
      document: drug,
    });
  }
}
