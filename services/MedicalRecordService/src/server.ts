import mongoose from 'mongoose';
import { config } from './config';
import { createApp } from './app';
import { RecordService } from './services/record-service';
import { RecordController } from './controllers/record-controller';
import { createRecordRouter } from './routes/record-routes';
import { createHealthRouter } from './routes/health-routes';
import { errorHandler } from './middleware/error-handler';
import { startConsumers } from './queue/rabbitmq-consumer';
import { createLogger } from '@hospital/shared';

const logger = createLogger(config.serviceName);

async function start(): Promise<void> {
  // Connect to MongoDB
  await mongoose.connect(config.mongoUri);
  logger.info('Connected to MongoDB');

  // Ensure indexes are created
  await mongoose.connection.syncIndexes();
  logger.info('MongoDB indexes synced');

  const app = createApp();

  // Dependency injection
  const recordService = new RecordService();
  const recordController = new RecordController(recordService);

  // Routes
  app.use('/api/medical-records', createRecordRouter(recordController));
  app.use('/', createHealthRouter());
  app.use(errorHandler);

  // Start event consumers (non-blocking)
  startConsumers(recordService, config.rabbitmq.url).catch(err =>
    logger.warn('Consumer startup failed', { error: err })
  );

  app.listen(config.port, () => logger.info(`MedicalRecordService listening on port ${config.port}`));
}

start().catch(err => {
  console.error('Fatal startup error', err);
  process.exit(1);
});
