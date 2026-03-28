import dotenv from 'dotenv';
dotenv.config();

export const config = {
  port: parseInt(process.env['PORT'] ?? '5006', 10),
  elasticsearch: {
    node: process.env['ELASTICSEARCH_URI'] ?? 'http://localhost:9200',
  },
  rabbitmq: {
    url: `amqp://${process.env['RABBITMQ_USER'] ?? 'guest'}:${process.env['RABBITMQ_PASSWORD'] ?? 'guest'}@${process.env['RABBITMQ_HOST'] ?? 'localhost'}:${process.env['RABBITMQ_PORT'] ?? '5672'}`,
  },
  serviceName: 'search-service',
} as const;
