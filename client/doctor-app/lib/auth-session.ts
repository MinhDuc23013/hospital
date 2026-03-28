// Server-side auth session helpers — use in Server Components and Route Handlers only.
// getAccessToken() decodes the raw JWT directly — NOT from the session callback,
// because the session callback intentionally omits accessToken for client-side security.
import { decode } from "next-auth/jwt";
import { headers } from "next/headers";
import { auth } from "@/lib/auth-config";
import type { Session } from "next-auth";

// Auth.js v5 splits large JWTs across multiple cookies: authjs.session-token.0, .1, ...
// This cookie name is the prefix used in development (no __Secure- prefix).
const SESSION_COOKIE = "authjs.session-token";

/** Parse Cookie header into a name→value map. */
function parseCookies(cookieHeader: string): Record<string, string> {
  const map: Record<string, string> = {};
  for (const part of cookieHeader.split(";")) {
    const eqIdx = part.indexOf("=");
    if (eqIdx === -1) continue;
    const name = part.slice(0, eqIdx).trim();
    const value = part.slice(eqIdx + 1).trim();
    map[name] = value;
  }
  return map;
}

/** Reassemble Auth.js v5 chunked session token from cookie map. */
function readSessionToken(cookies: Record<string, string>): string | null {
  // Try single cookie first
  if (cookies[SESSION_COOKIE]) return cookies[SESSION_COOKIE];
  // Reassemble chunks: authjs.session-token.0, .1, ...
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

  console.log("[auth] cookie keys:", Object.keys(cookies));
  console.log("[auth] session token found:", !!sessionToken, "length:", sessionToken?.length);

  try {
    const token = await decode({
      token: sessionToken,
      secret: process.env.NEXTAUTH_SECRET!,
      salt: SESSION_COOKIE,
    });
    console.log("[auth] decoded keys:", token ? Object.keys(token) : "null");
    console.log("[auth] accessToken present:", !!(token?.accessToken));
    return (token?.accessToken as string | undefined) ?? null;
  } catch (err) {
    console.error("[auth] decode error:", err);
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
