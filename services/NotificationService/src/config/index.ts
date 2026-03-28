import dotenv from 'dotenv';
dotenv.config();

export const config = {
  port: parseInt(process.env['PORT'] ?? '5005', 10),
  redis: {
    host: process.env['REDIS_HOST'] ?? 'localhost',
    port: parseInt(process.env['REDIS_PORT'] ?? '6379', 10),
  },
  rabbitmq: {
    url: `amqp://${process.env['RABBITMQ_USER'] ?? 'guest'}:${process.env['RABBITMQ_PASSWORD'] ?? 'guest'}@${process.env['RABBITMQ_HOST'] ?? 'localhost'}:${process.env['RABBITMQ_PORT'] ?? '5672'}`,
  },
  email: {
    host: process.env['SMTP_HOST'] ?? 'localhost',
    port: parseInt(process.env['SMTP_PORT'] ?? '587', 10),
    user: process.env['SMTP_USER'] ?? '',
    password: process.env['SMTP_PASSWORD'] ?? '',
    from: process.env['EMAIL_FROM'] ?? 'noreply@hospital.local',
  },
  serviceName: 'notification-service',
} as const;
