import { Router } from 'express';
import { healthHandler } from '../controllers/health-controller';

export function createHealthRouter(): Router {
  const router = Router();
  router.get('/health', healthHandler);
  return router;
}
