import { Router } from 'express';
import { SearchController } from '../controllers/search-controller';

export function createSearchRouter(controller: SearchController): Router {
  const router = Router();
  router.get('/', (req, res, next) => controller.search(req, res, next));
  return router;
}
