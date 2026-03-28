// Server-side API client — injects Bearer token from server session.
// NEVER import this in Client Components or expose it to the browser bundle.
// Use the /api/proxy/* route for client-side data fetching instead.
import { getAccessToken } from "@/lib/auth-session";

const GATEWAY_BASE_URL = process.env.GATEWAY_API_URL;

// Custom error types for structured error handling
export class AuthError extends Error {
  constructor(message = "Unauthenticated") {
    super(message);
    this.name = "AuthError";
  }
}

export class GatewayError extends Error {
  constructor(
    message: string,
    public readonly status: number,
    public readonly correlationId?: string
  ) {
    super(message);
    this.name = "GatewayError";
  }
}

/**
 * Call the API Gateway from a Server Component or Route Handler.
 * Automatically injects Authorization: Bearer <accessToken> header.
 * Throws AuthError on 401, GatewayError on other 4xx/5xx responses.
 */
export async function callGatewayAPI<T>(
  endpoint: string,
  options: RequestInit = {}
): Promise<T> {
  if (!GATEWAY_BASE_URL) {
    throw new Error("GATEWAY_API_URL env var is not configured");
  }

  const accessToken = await getAccessToken();
  if (!accessToken) {
    throw new AuthError("No access token available — user may not be logged in");
  }

  const url = `${GATEWAY_BASE_URL}${endpoint.startsWith("/") ? endpoint : `/${endpoint}`}`;

  const response = await fetch(url, {
    ...options,
    // Authenticated data must not be cached at the CDN or Next.js data cache level
    cache: "no-store",
    headers: {
      "Content-Type": "application/json",
      ...options.headers,
      Authorization: `Bearer ${accessToken}`,
    },
  });

  if (response.status === 401) {
    throw new AuthError("Session expired or invalid token");
  }

  if (!response.ok) {
    // Attempt to parse structured error body from gateway
    let message = `Gateway error ${response.status}`;
    let correlationId: string | undefined;
    try {
      const body = (await response.json()) as {
        error?: { message?: string; correlationId?: string };
      };
      if (body.error?.message) message = body.error.message;
      correlationId = body.error?.correlationId;
    } catch {
      // ignore parse failure — use default message
    }
    throw new GatewayError(message, response.status, correlationId);
  }

  return response.json() as Promise<T>;
}
