import { Request, Response, NextFunction } from 'express';
import { SearchService } from '../services/search-service';

export class SearchController {
  constructor(private readonly searchService: SearchService) {}

  async search(req: Request, res: Response, next: NextFunction): Promise<void> {
    try {
      const q = req.query['q'] as string;
      if (!q) {
        res.status(400).json({ error: { message: 'Query parameter "q" is required' } });
        return;
      }
      const type = (req.query['type'] as 'patient' | 'drug') ?? 'patient';
      const page = parseInt(req.query['page'] as string) || 1;
      const pageSize = parseInt(req.query['pageSize'] as string) || 50;

      const result = await this.searchService.search(q, type, page, pageSize);
      res.json({
        data: result.items,
        pagination: { total: result.total, page: result.page, pageSize: result.pageSize },
      });
    } catch (err) {
      next(err);
    }
  }
}
