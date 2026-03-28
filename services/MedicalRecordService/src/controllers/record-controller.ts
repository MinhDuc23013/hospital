import { Request, Response, NextFunction } from 'express';
import { RecordService } from '../services/record-service';

export class RecordController {
  constructor(private readonly recordService: RecordService) {}

  async create(req: Request, res: Response, next: NextFunction): Promise<void> {
    try {
      const record = await this.recordService.create(req.body);
      res.status(201).json({ data: record });
    } catch (err) { next(err); }
  }

  async getByPatientId(req: Request, res: Response, next: NextFunction): Promise<void> {
    try {
      const { patientId } = req.params;
      const page = parseInt(req.query['page'] as string) || 1;
      const pageSize = parseInt(req.query['pageSize'] as string) || 50;
      const result = await this.recordService.getByPatientId(patientId, page, pageSize);
      res.json({ data: result.items, pagination: { total: result.total, page: result.page, pageSize: result.pageSize } });
    } catch (err) { next(err); }
  }

  async getById(req: Request, res: Response, next: NextFunction): Promise<void> {
    try {
      const record = await this.recordService.getById(req.params['id']!);
      if (!record) { res.status(404).json({ error: { message: 'Record not found' } }); return; }
      res.json({ data: record });
    } catch (err) { next(err); }
  }

  async update(req: Request, res: Response, next: NextFunction): Promise<void> {
    try {
      const record = await this.recordService.update(req.params['id']!, req.body);
      if (!record) { res.status(404).json({ error: { message: 'Record not found' } }); return; }
      res.json({ data: record });
    } catch (err) { next(err); }
  }
}
