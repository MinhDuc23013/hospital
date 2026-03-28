import { Router } from 'express';
import { NotificationController } from '../controllers/notification-controller';

export function createNotificationRouter(controller: NotificationController): Router {
  const router = Router();
  router.post('/send', (req, res, next) => controller.send(req, res, next));
  router.get('/logs', (req, res, next) => controller.getLogs(req, res, next));
  return router;
}
