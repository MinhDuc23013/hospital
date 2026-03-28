// NextAuth v5 type augmentation — extended in Phase 2 with Keycloak claims.
// Keycloak JWT delivers roles at top-level `token.roles[]` (confirmed in plan validation).
// Patient ID = Keycloak sub (confirmed: Keycloak sub === Patient Service UUID).
import type { DefaultSession } from "next-auth";

declare module "next-auth" {
  interface Session {
    user: {
      /** Keycloak subject — used as Patient ID in all API calls */
      id: string;
      /** Top-level roles array from Keycloak JWT */
      roles: string[];
    } & DefaultSession["user"];
    /** Opaque access token forwarded to API Gateway (server-side only) */
    accessToken?: string;
    /** Token expiry timestamp (seconds) */
    expiresAt?: number;
    error?: "RefreshTokenError";
  }

  interface User {
    roles?: string[];
  }
}

declare module "next-auth/jwt" {
  interface JWT {
    /** Keycloak access token — server-side only, never exposed to client bundle */
    accessToken?: string;
    refreshToken?: string;
    /** Unix timestamp (seconds) when accessToken expires */
    expiresAt?: number;
    /** Keycloak id_token — stored for end_session logout revocation [AUDIT FIX F11] */
    idToken?: string;
    roles?: string[];
    error?: "RefreshTokenError";
  }
}
