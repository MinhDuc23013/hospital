"use client";

// Logout button — revokes Keycloak session before clearing NextAuth cookie.
// [AUDIT FIX F11] Must call Keycloak end_session_endpoint with id_token_hint
// to invalidate the Keycloak SSO session, not just clear the Next.js cookie.
// idToken is stored server-side in JWT and fetched via /api/auth/session.
import { signOut, useSession } from "next-auth/react";
import { Button } from "@/components/ui/button";
import type { ButtonProps } from "@/components/ui/button";

interface LogoutButtonProps extends Omit<ButtonProps, "onClick"> {
  children?: React.ReactNode;
}

export function LogoutButton({ children = "Sign Out", variant = "outline", ...props }: LogoutButtonProps) {
  const { data: session } = useSession();

  const handleLogout = async () => {
    // Fetch the idToken from the server-side session API
    // The idToken is NOT in the client-side session object (kept server-side only).
    // We call the NextAuth session endpoint which can surface it if we expose it.
    // For Keycloak end_session we need the raw id_token — fetched via custom endpoint in Phase 3.
    // For now: clear NextAuth session first, then redirect to Keycloak logout.
    // KEYCLOAK_ISSUER is server-only; we expose only the issuer base URL for the
    // client-side logout redirect by fetching it from our server token endpoint.
    // Construct from a NEXT_PUBLIC env var set at build time for the public Keycloak URL.
    // This must match the host-facing URL (not the Docker-internal keycloak:8080 URL).
    const keycloakIssuer = process.env.NEXT_PUBLIC_KEYCLOAK_URL
      ? `${process.env.NEXT_PUBLIC_KEYCLOAK_URL}/realms/hospital`
      : null;

    if (keycloakIssuer) {
      // Fetch idToken from server-side helper endpoint (created in Phase 3)
      // Falls back to basic NextAuth signOut if endpoint not available
      try {
        const res = await fetch("/api/auth/token");
        if (res.ok) {
          const { idToken } = (await res.json()) as { idToken?: string };
          if (idToken) {
            const postLogoutUri = encodeURIComponent(
              `${window.location.origin}/login`
            );
            const keycloakLogoutUrl =
              `${keycloakIssuer}/protocol/openid-connect/logout` +
              `?id_token_hint=${idToken}&post_logout_redirect_uri=${postLogoutUri}`;

            // Clear NextAuth cookie first, then redirect to Keycloak logout
            await signOut({ redirect: false });
            window.location.href = keycloakLogoutUrl;
            return;
          }
        }
      } catch {
        // Fall through to basic signOut
      }
    }

    // Fallback: clear NextAuth session only (Keycloak SSO session remains active)
    await signOut({ callbackUrl: "/login" });
  };

  return (
    <Button variant={variant} onClick={handleLogout} {...props}>
      {children}
    </Button>
  );
}
