import { Client } from '@elastic/elasticsearch';
import { config } from './config';
import { createApp } from './app';
import { IndexerService } from './services/indexer-service';
import { SearchService } from './services/search-service';
import { SearchController } from './controllers/search-controller';
import { createSearchRouter } from './routes/search-routes';
import { createHealthRouter } from './routes/health-routes';
import { errorHandler } from './middleware/error-handler';
import { startConsumers } from './queue/rabbitmq-consumer';
import { createLogger } from '@hospital/shared';

const logger = createLogger(config.serviceName);

async function start(): Promise<void> {
  const esClient = new Client({ node: config.elasticsearch.node });

  const indexerService = new IndexerService(esClient);
  // ensureIndices is best-effort — ES may not be available at startup
  indexerService.ensureIndices().catch(err =>
    logger.warn('Elasticsearch not available at startup, indices not created', { error: err }),
  );

  const searchService = new SearchService(esClient);
  const searchController = new SearchController(searchService);

  const app = createApp();
  app.use('/api/search', createSearchRouter(searchController));
  app.use('/', createHealthRouter());
  app.use(errorHandler);

  startConsumers(indexerService, config.rabbitmq.url).catch(err =>
    logger.warn('Consumer startup failed', { error: err }),
  );

  app.listen(config.port, () =>
    logger.info(`SearchService listening on port ${config.port}`),
  );
}

start().catch(err => {
  console.error('Fatal startup error', err);
  process.exit(1);
});
