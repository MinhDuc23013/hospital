import Redis from 'ioredis';
import { createLogger } from '@hospital/shared';
import { EmailService } from './email-service';
import { SmsService } from './sms-service';

interface NotificationData {
  subject?: string;
  body?: string;
  message?: string;
}

interface NotificationLog {
  type: string;
  recipient: string;
  data: NotificationData;
  sentAt: string;
  status: string;
}

const logger = createLogger('notification-service');

export class NotificationService {
  constructor(
    private readonly emailService: EmailService,
    private readonly smsService: SmsService,
    private readonly redis: Redis,
  ) {}

  async sendNotification(
    type: 'email' | 'sms',
    recipient: string,
    data: NotificationData,
  ): Promise<NotificationLog> {
    const logEntry: NotificationLog = {
      type,
      recipient,
      data,
      sentAt: new Date().toISOString(),
      status: 'sent',
    };

    // Store in Redis for history — keep last 1000 logs
    await this.redis.lpush('notification:logs', JSON.stringify(logEntry));
    await this.redis.ltrim('notification:logs', 0, 999);

    if (type === 'email' && data.subject && data.body) {
      await this.emailService.send(recipient, data.subject, data.body);
    } else if (type === 'sms' && data.message) {
      await this.smsService.send(recipient, data.message);
    }

    logger.info('Notification sent', { type, recipient });
    return logEntry;
  }

  async getRecentLogs(limit = 50): Promise<NotificationLog[]> {
    const logs = await this.redis.lrange('notification:logs', 0, limit - 1);
    return logs.map(l => JSON.parse(l) as NotificationLog);
  }
}
