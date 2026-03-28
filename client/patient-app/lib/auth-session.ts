// Server-side auth session helpers — use in Server Components and Route Handlers only.
// getAccessToken() decodes the raw JWT directly — NOT from the session callback,
// because the session callback intentionally omits accessToken for client-side security.
import { decode } from "next-auth/jwt";
import { headers } from "next/headers";
import { auth } from "@/lib/auth-config";
import type { Session } from "next-auth";

const SESSION_COOKIE = "authjs.session-token";

function parseCookies(cookieHeader: string): Record<string, string> {
  const map: Record<string, string> = {};
  for (const part of cookieHeader.split(";")) {
    const eqIdx = part.indexOf("=");
    if (eqIdx === -1) continue;
    map[part.slice(0, eqIdx).trim()] = part.slice(eqIdx + 1).trim();
  }
  return map;
}

function readSessionToken(cookies: Record<string, string>): string | null {
  if (cookies[SESSION_COOKIE]) return cookies[SESSION_COOKIE];
  const chunks: string[] = [];
  for (let i = 0; ; i++) {
    const chunk = cookies[`${SESSION_COOKIE}.${i}`];
    if (!chunk) break;
    chunks.push(chunk);
  }
  return chunks.length > 0 ? chunks.join("") : null;
}

/**
 * Get the current authenticated session in a Server Component or Route Handler.
 * Returns null if the user is not authenticated.
 */
export async function getAuthSession(): Promise<Session | null> {
  return auth();
}

/**
 * Get the raw access token from the JWT for Bearer authorization headers.
 * Server-side only — never call from Client Components.
 * Returns null if not authenticated or token unavailable.
 *
 * Manually reassembles Auth.js v5 chunked cookies and decodes the JWE directly,
 * because getToken() (from next-auth/jwt v4) doesn't handle chunked authjs.* cookies.
 */
export async function getAccessToken(): Promise<string | null> {
  const cookieHeader = headers().get("cookie") ?? "";
  const cookies = parseCookies(cookieHeader);
  const sessionToken = readSessionToken(cookies);
  if (!sessionToken) return null;

  try {
    const token = await decode({
      token: sessionToken,
      secret: process.env.NEXTAUTH_SECRET!,
      salt: SESSION_COOKIE,
    });
    return (token?.accessToken as string | undefined) ?? null;
  } catch {
    return null;
  }
}

/**
 * Assert the user is authenticated. Throws if not — use in protected Server Actions.
 */
export async function requireAuthSession(): Promise<Session> {
  const session = await getAuthSession();
  if (!session) {
    throw new Error("Unauthenticated: no active session");
  }
  return session;
}
