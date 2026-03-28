// NextAuth v5 configuration with Keycloak OIDC provider
// Security notes:
// - accessToken is kept server-side in JWT only (never exposed to client session)
// - idToken stored in JWT for Keycloak end_session logout revocation
// - Roles extracted from top-level `token.roles[]` (confirmed in plan validation)
import NextAuth from "next-auth";
import Keycloak from "next-auth/providers/keycloak";
import { refreshAccessTokenSafe } from "@/lib/auth-refresh";
import type { NextAuthConfig } from "next-auth";

export const authConfig: NextAuthConfig = {
  providers: [
    Keycloak({
      clientId: process.env.KEYCLOAK_CLIENT_ID!,
      clientSecret: process.env.KEYCLOAK_CLIENT_SECRET!,
      issuer: process.env.KEYCLOAK_ISSUER!,
      authorization: {
        params: { scope: "openid email profile" },
      },
    }),
  ],

  session: {
    strategy: "jwt",
    maxAge: 24 * 60 * 60, // 24 hours
  },

  pages: {
    signIn: "/login",
    error: "/error",
  },

  callbacks: {
    async jwt({ token, account, profile }) {
      // Initial sign-in: persist tokens from Keycloak
      if (account && profile) {
        return {
          ...token,
          accessToken: account.access_token,
          refreshToken: account.refresh_token,
          idToken: account.id_token,
          expiresAt: account.expires_at,
          // Roles at top-level claim (confirmed: not resource_access)
          roles: (profile as Record<string, unknown>).roles as string[] ?? [],
        };
      }

      // Token still valid — return as-is
      const nowSeconds = Math.floor(Date.now() / 1000);
      if (token.expiresAt && nowSeconds < token.expiresAt - 30) {
        return token;
      }

      // Token expired — refresh with mutex to prevent race conditions
      return refreshAccessTokenSafe(token);
    },

    async session({ session, token }) {
      // Expose only non-sensitive fields to client-side session
      // accessToken deliberately excluded — use getAccessToken() in server components
      session.user.id = token.sub ?? "";
      session.user.roles = token.roles ?? [];
      if (token.error) {
        (session as unknown as Record<string, unknown>).error = token.error;
      }
      return session;
    },
  },
};

export const { handlers, auth, signIn, signOut } = NextAuth(authConfig);
