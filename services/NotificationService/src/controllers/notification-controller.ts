import { Request, Response, NextFunction } from 'express';
import { NotificationService } from '../services/notification-service';

export class NotificationController {
  constructor(private readonly notificationService: NotificationService) {}

  async send(req: Request, res: Response, next: NextFunction): Promise<void> {
    try {
      const { type, recipient, data } = req.body;
      const result = await this.notificationService.sendNotification(type, recipient, data);
      res.status(201).json({ data: result });
    } catch (err) {
      next(err);
    }
  }

  async getLogs(req: Request, res: Response, next: NextFunction): Promise<void> {
    try {
      const limit = parseInt(req.query['limit'] as string) || 50;
      const logs = await this.notificationService.getRecentLogs(limit);
      res.json({ data: logs });
    } catch (err) {
      next(err);
    }
  }
}
