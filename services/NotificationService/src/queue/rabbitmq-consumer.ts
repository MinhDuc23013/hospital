import { RabbitMQClient, EventNames, PatientCreatedEvent, AppointmentScheduledEvent, createLogger } from '@hospital/shared';
import { NotificationService } from '../services/notification-service';

const logger = createLogger('notification-service');

export async function startConsumers(
  notificationService: NotificationService,
  rabbitmqUrl: string,
): Promise<void> {
  const client = new RabbitMQClient(rabbitmqUrl);
  try {
    await client.connect();

    await client.subscribe(
      'hospital.events',
      EventNames.PatientCreated,
      'notification.patient-created',
      async (msg: unknown) => {
        const event = msg as PatientCreatedEvent;
        await notificationService.sendNotification('email', event.email, {
          subject: 'Welcome to Hospital HRM',
          body: `Hello ${event.firstName}, welcome to our hospital system.`,
        });
      },
    );

    await client.subscribe(
      'hospital.events',
      EventNames.AppointmentScheduled,
      'notification.appointment-scheduled',
      async (_msg: unknown) => {
        // Stub: appointment reminder — implement with full details in production
        const _event = _msg as AppointmentScheduledEvent;
        logger.info('AppointmentScheduled event received — reminder stub');
      },
    );

    logger.info('RabbitMQ consumers started');
  } catch (err) {
    logger.warn('RabbitMQ not available at startup, consumers not started', { error: err });
  }
}
