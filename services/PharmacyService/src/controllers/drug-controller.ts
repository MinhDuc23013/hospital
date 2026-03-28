import { Request, Response, NextFunction } from 'express';
import { DrugService } from '../services/drug-service';

export class DrugController {
  constructor(private readonly drugService: DrugService) {}

  async list(req: Request, res: Response, next: NextFunction): Promise<void> {
    try {
      const page = parseInt(req.query['page'] as string) || 1;
      const pageSize = parseInt(req.query['pageSize'] as string) || 50;
      const search = req.query['search'] as string | undefined;
      const { items, total } = await this.drugService.list(page, pageSize, search);
      res.json({ data: items, pagination: { total, page, pageSize } });
    } catch (err) { next(err); }
  }

  async getById(req: Request, res: Response, next: NextFunction): Promise<void> {
    try {
      const drug = await this.drugService.getById(parseInt(req.params['id']!));
      if (!drug) { res.status(404).json({ error: { message: 'Drug not found' } }); return; }
      res.json({ data: drug });
    } catch (err) { next(err); }
  }

  async getLowStock(_req: Request, res: Response, next: NextFunction): Promise<void> {
    try {
      const drugs = await this.drugService.getLowStock();
      res.json({ data: drugs });
    } catch (err) { next(err); }
  }
}
