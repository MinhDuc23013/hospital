import { Request, Response, NextFunction } from 'express';
import { PrescriptionService } from '../services/prescription-service';

export class PrescriptionController {
  constructor(private readonly prescriptionService: PrescriptionService) {}

  async create(req: Request, res: Response, next: NextFunction): Promise<void> {
    try {
      const prescription = await this.prescriptionService.create(req.body);
      res.status(201).json({ data: prescription });
    } catch (err) { next(err); }
  }

  async getById(req: Request, res: Response, next: NextFunction): Promise<void> {
    try {
      const prescription = await this.prescriptionService.getById(req.params['id']!);
      if (!prescription) { res.status(404).json({ error: { message: 'Prescription not found' } }); return; }
      res.json({ data: prescription });
    } catch (err) { next(err); }
  }

  async dispense(req: Request, res: Response, next: NextFunction): Promise<void> {
    try {
      const prescription = await this.prescriptionService.dispense(req.params['id']!);
      if (!prescription) { res.status(404).json({ error: { message: 'Prescription not found' } }); return; }
      res.json({ data: prescription });
    } catch (err) { next(err); }
  }
}
