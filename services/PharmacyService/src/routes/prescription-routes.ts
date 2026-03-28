import { Router } from 'express';
import { PrescriptionController } from '../controllers/prescription-controller';

export function createPrescriptionRouter(controller: PrescriptionController): Router {
  const router = Router();
  router.post('/', (req, res, next) => controller.create(req, res, next));
  router.get('/:id', (req, res, next) => controller.getById(req, res, next));
  router.put('/:id/dispense', (req, res, next) => controller.dispense(req, res, next));
  return router;
}
