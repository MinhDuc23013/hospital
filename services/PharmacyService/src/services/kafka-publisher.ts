import { Kafka, Producer } from 'kafkajs';
import { createLogger } from '@hospital/shared';

const logger = createLogger('kafka-publisher');

/**
 * Kafka producer for publishing domain events (audit/trace).
 * Topic naming: hospital.{event-name-kebab-case}
 */
export class KafkaPublisher {
  private producer: Producer;
  private connected = false;

  constructor(brokers: string[]) {
    const kafka = new Kafka({
      clientId: 'pharmacy-service',
      brokers,
    });
    this.producer = kafka.producer();
  }

  async connect(): Promise<void> {
    await this.producer.connect();
    this.connected = true;
    logger.info('Kafka producer connected');
  }

  async publish(eventName: string, data: Record<string, unknown>): Promise<void> {
    if (!this.connected) {
      logger.warn('Kafka producer not connected, skipping publish');
      return;
    }

    const topic = `hospital.${eventName.replace(/([A-Z])/g, '-$1').toLowerCase().replace(/^-/, '')}`;

    await this.producer.send({
      topic,
      messages: [{ key: crypto.randomUUID(), value: JSON.stringify(data) }],
    });

    logger.info(`Published ${eventName} to Kafka topic ${topic}`);
  }

  async disconnect(): Promise<void> {
    await this.producer.disconnect();
    this.connected = false;
  }
}
