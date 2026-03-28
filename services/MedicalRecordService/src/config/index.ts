import dotenv from 'dotenv';
dotenv.config();

export const config = {
  port: parseInt(process.env['PORT'] ?? '5003', 10),
  mongoUri: process.env['MONGODB_URI'] ?? 'mongodb://localhost:27017/hospital',
  rabbitmq: {
    url: `amqp://${process.env['RABBITMQ_USER'] ?? 'guest'}:${process.env['RABBITMQ_PASSWORD'] ?? 'guest'}@${process.env['RABBITMQ_HOST'] ?? 'localhost'}:${process.env['RABBITMQ_PORT'] ?? '5672'}`,
  },
  serviceName: 'medical-record-service',
} as const;
