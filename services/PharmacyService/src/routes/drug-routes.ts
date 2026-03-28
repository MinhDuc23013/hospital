import { Router } from 'express';
import { DrugController } from '../controllers/drug-controller';

export function createDrugRouter(controller: DrugController): Router {
  const router = Router();
  router.get('/', (req, res, next) => controller.list(req, res, next));
  router.get('/low-stock', (req, res, next) => controller.getLowStock(req, res, next));
  router.get('/:id', (req, res, next) => controller.getById(req, res, next));
  return router;
}
