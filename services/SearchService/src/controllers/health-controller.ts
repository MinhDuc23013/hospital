import { Request, Response } from 'express';

export const healthHandler = (_req: Request, res: Response): void => {
  res.json({
    service: 'search-service',
    status: 'healthy',
    timestamp: new Date().toISOString(),
  });
};
