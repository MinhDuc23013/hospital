import type Keycloak from 'keycloak-js';

const ACCESS_KEY = 'hrm_access_token';
const REFRESH_KEY = 'hrm_refresh_token';

export interface StoredSession {
  token?: string;
  refreshToken?: string;
  authenticated: boolean;
}

/** Read tokens from localStorage and decode JWT payload. */
export function restoreSession(keycloak: Keycloak): StoredSession {
  const token = localStorage.getItem(ACCESS_KEY) ?? undefined;
  const refreshToken = localStorage.getItem(REFRESH_KEY) ?? undefined;

  if (!token) return { authenticated: false };

  try {
    const payload = parseJwt(token);
    if (!payload) return { authenticated: false };

    // Check expiry
    if (payload.exp && payload.exp * 1000 < Date.now()) {
      // Expired — will be refreshed by interceptor when first request runs
      return { token, refreshToken, authenticated: !!refreshToken };
    }

    // Inject into keycloak-js state so useAuth() returns correct data
    keycloak.token = token;
    keycloak.refreshToken = refreshToken;
    keycloak.tokenParsed = payload;
    keycloak.authenticated = true;

    return { token, refreshToken, authenticated: true };
  } catch (err) {
    console.error('Failed to restore session', err);
    return { authenticated: false };
  }
}

/** Save tokens to localStorage and update keycloak-js state. */
export function saveSession(keycloak: Keycloak, tokens: {
  access_token: string;
  refresh_token: string;
  expires_in?: number;
}): void {
  localStorage.setItem(ACCESS_KEY, tokens.access_token);
  localStorage.setItem(REFRESH_KEY, tokens.refresh_token);

  keycloak.token = tokens.access_token;
  keycloak.refreshToken = tokens.refresh_token;
  keycloak.tokenParsed = parseJwt(tokens.access_token);
  keycloak.authenticated = true;
}

/** Clear session from storage and keycloak-js state. */
export function clearSession(keycloak: Keycloak): void {
  localStorage.removeItem(ACCESS_KEY);
  localStorage.removeItem(REFRESH_KEY);
  keycloak.token = undefined;
  keycloak.refreshToken = undefined;
  keycloak.tokenParsed = undefined;
  keycloak.authenticated = false;
}

/** Parse JWT payload (base64url-decoded middle segment). */
function parseJwt(token: string): any {
  try {
    const parts = token.split('.');
    if (parts.length !== 3) return null;
    const base64 = parts[1].replace(/-/g, '+').replace(/_/g, '/');
    const json = decodeURIComponent(
      atob(base64)
        .split('')
        .map(c => '%' + ('00' + c.charCodeAt(0).toString(16)).slice(-2))
        .join(''),
    );
    return JSON.parse(json);
  } catch {
    return null;
  }
}
