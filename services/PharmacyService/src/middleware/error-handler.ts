import { Request, Response, NextFunction } from 'express';

interface ApiError extends Error {
  statusCode?: number;
  code?: string;
}

export function errorHandler(err: ApiError, _req: Request, res: Response, _next: NextFunction): void {
  const status = err.statusCode ?? 500;
  res.status(status).json({
    error: {
      message: err.message,
      code: err.code ?? 'INTERNAL_ERROR',
    },
  });
}
