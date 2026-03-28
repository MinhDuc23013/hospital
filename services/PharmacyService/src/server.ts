import { config } from './config';
import { createApp } from './app';
import { sequelize } from './database/sequelize';
import { DrugService } from './services/drug-service';
import { PrescriptionService } from './services/prescription-service';
import { KafkaPublisher } from './services/kafka-publisher';
import { DrugController } from './controllers/drug-controller';
import { PrescriptionController } from './controllers/prescription-controller';
import { createDrugRouter } from './routes/drug-routes';
import { createPrescriptionRouter } from './routes/prescription-routes';
import { createHealthRouter } from './routes/health-routes';
import { errorHandler } from './middleware/error-handler';
import { createLogger } from '@hospital/shared';

// Import models to register them with sequelize before sync
import './models/drug';
import './models/prescription';

const logger = createLogger(config.serviceName);

async function start(): Promise<void> {
  // Connect and sync Sequelize models (dev only — use migrations in prod)
  await sequelize.authenticate();
  logger.info('Connected to SQL Server');
  await sequelize.sync({ alter: true });
  logger.info('Sequelize models synced');

  // Kafka producer for audit/trace events
  const kafkaPublisher = new KafkaPublisher(config.kafka.brokers);
  await kafkaPublisher.connect().catch(err => {
    logger.warn('Kafka connection failed, events will be skipped', { error: err });
  });

  const app = createApp();

  const drugService = new DrugService();
  const prescriptionService = new PrescriptionService(kafkaPublisher);
  const drugController = new DrugController(drugService);
  const prescriptionController = new PrescriptionController(prescriptionService);

  app.use('/api/drugs', createDrugRouter(drugController));
  app.use('/api/prescriptions', createPrescriptionRouter(prescriptionController));
  app.use('/', createHealthRouter());
  app.use(errorHandler);

  app.listen(config.port, () => logger.info(`PharmacyService listening on port ${config.port}`));

  // Graceful shutdown
  process.on('SIGTERM', async () => {
    await kafkaPublisher.disconnect();
    process.exit(0);
  });
}

start().catch(err => {
  console.error('Fatal startup error', err);
  process.exit(1);
});
