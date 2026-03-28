import express from 'express';
import cors from 'cors';
import helmet from 'helmet';

export function createApp(): express.Application {
  const app = express();
  app.use(helmet());
  app.use(cors());
  app.use(express.json());
  return app;
}
