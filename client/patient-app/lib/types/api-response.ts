// Shared API response shapes — used by api-client.ts and proxy route handlers.

export interface ApiResponse<T> {
  data: T;
  timestamp: string;
}

export interface PaginatedResponse<T> {
  data: T[];
  pagination: {
    total: number;
    page: number;
    pageSize: number;
    totalPages: number;
  };
}

export interface ApiError {
  error: {
    message: string;
    status: number;
    timestamp: string;
    correlationId?: string;
  };
}
