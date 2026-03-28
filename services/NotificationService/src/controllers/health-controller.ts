import { Request, Response } from 'express';

export const healthHandler = (_req: Request, res: Response): void => {
  res.json({
    service: 'notification-service',
    status: 'healthy',
    timestamp: new Date().toISOString(),
  });
};
