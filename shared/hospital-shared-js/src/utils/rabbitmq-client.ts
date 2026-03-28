import amqplib, { Channel, ChannelModel } from 'amqplib';
import { createLogger } from './logger';

const logger = createLogger('rabbitmq-client');

/** RabbitMQ connection wrapper with retry logic and channel management */
export class RabbitMQClient {
  private model: ChannelModel | null = null;
  private channel: Channel | null = null;

  constructor(private readonly url: string) {}

  /** Connect with exponential backoff retry (max 5 attempts) */
  async connect(retries = 5): Promise<void> {
    for (let attempt = 1; attempt <= retries; attempt++) {
      try {
        this.model = await amqplib.connect(this.url);
        this.channel = await this.model.createChannel();
        logger.info('RabbitMQ connected');
        return;
      } catch (err) {
        logger.warn(`RabbitMQ connect attempt ${attempt}/${retries} failed`);
        if (attempt === retries) throw err;
        await new Promise(r => setTimeout(r, attempt * 2000));
      }
    }
  }

  /** Publish a message to a named exchange */
  async publish(exchange: string, routingKey: string, message: unknown): Promise<void> {
    if (!this.channel) throw new Error('RabbitMQ channel not initialized');
    await this.channel.assertExchange(exchange, 'topic', { durable: true });
    this.channel.publish(exchange, routingKey, Buffer.from(JSON.stringify(message)));
  }

  /** Subscribe to messages from a queue bound to an exchange */
  async subscribe(
    exchange: string,
    routingKey: string,
    queue: string,
    handler: (msg: unknown) => Promise<void>
  ): Promise<void> {
    if (!this.channel) throw new Error('RabbitMQ channel not initialized');
    await this.channel.assertExchange(exchange, 'topic', { durable: true });
    await this.channel.assertQueue(queue, { durable: true });
    await this.channel.bindQueue(queue, exchange, routingKey);
    this.channel.consume(queue, async (msg) => {
      if (!msg) return;
      try {
        const content = JSON.parse(msg.content.toString()) as unknown;
        await handler(content);
        this.channel!.ack(msg);
      } catch (err) {
        logger.error('Message processing failed', { error: err });
        this.channel!.nack(msg, false, false); // send to DLQ
      }
    });
  }

  async close(): Promise<void> {
    await this.channel?.close();
    await this.model?.close();
    logger.info('RabbitMQ disconnected');
  }
}
