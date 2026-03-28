import { Router } from 'express';
import { RecordController } from '../controllers/record-controller';

export function createRecordRouter(controller: RecordController): Router {
  const router = Router();
  router.get('/patient/:patientId', (req, res, next) => controller.getByPatientId(req, res, next));
  router.get('/:id', (req, res, next) => controller.getById(req, res, next));
  router.post('/', (req, res, next) => controller.create(req, res, next));
  router.put('/:id', (req, res, next) => controller.update(req, res, next));
  return router;
}
