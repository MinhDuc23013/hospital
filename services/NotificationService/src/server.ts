import Redis from 'ioredis';
import { config } from './config';
import { createApp } from './app';
import { EmailService } from './services/email-service';
import { SmsService } from './services/sms-service';
import { NotificationService } from './services/notification-service';
import { NotificationController } from './controllers/notification-controller';
import { createNotificationRouter } from './routes/notification-routes';
import { createHealthRouter } from './routes/health-routes';
import { errorHandler } from './middleware/error-handler';
import { startConsumers } from './queue/rabbitmq-consumer';
import { createLogger } from '@hospital/shared';

const logger = createLogger(config.serviceName);

async function start(): Promise<void> {
  const redis = new Redis({ host: config.redis.host, port: config.redis.port });
  redis.on('error', err => logger.error('Redis error', { error: err }));

  const app = createApp();
  const emailService = new EmailService();
  const smsService = new SmsService();
  const notificationService = new NotificationService(emailService, smsService, redis);
  const notificationController = new NotificationController(notificationService);

  app.use('/api/notifications', createNotificationRouter(notificationController));
  app.use('/', createHealthRouter());
  app.use(errorHandler);

  startConsumers(notificationService, config.rabbitmq.url).catch(err =>
    logger.warn('Consumer startup failed', { error: err }),
  );

  app.listen(config.port, () =>
    logger.info(`NotificationService listening on port ${config.port}`),
  );
}

start().catch(err => {
  console.error('Fatal startup error', err);
  process.exit(1);
});
