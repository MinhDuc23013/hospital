// Token refresh utility for Keycloak OIDC
// Keycloak refresh tokens are single-use — concurrent JWT callbacks (parallel Server Components)
// would all fire simultaneously and invalidate the token after the first use.
// The refreshLocks map acts as an in-process mutex per user session.
import type { JWT } from "next-auth/jwt";

interface KeycloakTokenResponse {
  access_token: string;
  refresh_token: string;
  expires_in: number;
  id_token?: string;
  token_type: string;
}

async function refreshAccessToken(token: JWT): Promise<JWT> {
  try {
    const issuer = process.env.KEYCLOAK_ISSUER;
    const clientId = process.env.KEYCLOAK_CLIENT_ID;
    const clientSecret = process.env.KEYCLOAK_CLIENT_SECRET;

    if (!issuer || !clientId || !clientSecret) {
      throw new Error("Missing Keycloak environment variables");
    }

    const tokenEndpoint = `${issuer}/protocol/openid-connect/token`;

    const response = await fetch(tokenEndpoint, {
      method: "POST",
      headers: { "Content-Type": "application/x-www-form-urlencoded" },
      body: new URLSearchParams({
        grant_type: "refresh_token",
        client_id: clientId,
        client_secret: clientSecret,
        refresh_token: token.refreshToken ?? "",
      }),
    });

    if (!response.ok) {
      const errorBody = await response.text();
      console.error("[auth-refresh] Token refresh failed:", response.status, errorBody);
      throw new Error(`Refresh failed with status ${response.status}`);
    }

    const refreshed = (await response.json()) as KeycloakTokenResponse;

    return {
      ...token,
      accessToken: refreshed.access_token,
      refreshToken: refreshed.refresh_token ?? token.refreshToken,
      expiresAt: Math.floor(Date.now() / 1000) + refreshed.expires_in,
      // Preserve idToken for Keycloak end_session logout if new one not provided
      idToken: refreshed.id_token ?? token.idToken,
      error: undefined,
    };
  } catch (error) {
    console.error("[auth-refresh] refreshAccessToken error:", error);
    return {
      ...token,
      error: "RefreshTokenError",
    };
  }
}

// Per-user refresh lock — prevents concurrent refresh races for single-use tokens
const refreshLocks = new Map<string, Promise<JWT>>();

export async function refreshAccessTokenSafe(token: JWT): Promise<JWT> {
  const key = token.sub ?? "default";
  if (!refreshLocks.has(key)) {
    const p = refreshAccessToken(token).finally(() => refreshLocks.delete(key));
    refreshLocks.set(key, p);
  }
  return refreshLocks.get(key)!;
}
