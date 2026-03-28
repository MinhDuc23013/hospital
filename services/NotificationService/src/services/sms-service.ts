import { createLogger } from '@hospital/shared';

const logger = createLogger('notification-service');

/** SMS service stub — logs only in scaffold. Replace with Twilio in production. */
export class SmsService {
  async send(phoneNumber: string, message: string): Promise<void> {
    logger.info('SMS stub — would send SMS', { phoneNumber, message: message.substring(0, 50) });
  }
}
