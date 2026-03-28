// NextAuth v5 middleware — replaces Phase 1 placeholder
// Checks for a valid session on every non-public route.
// Role enforcement is handled by Keycloak at login (YAGNI — no per-route role check).
// Note: withAuth was removed in NextAuth v5; use auth directly as middleware.
export { auth as middleware } from "@/lib/auth-config";

export const config = {
  matcher: [
    // Run on all routes except NextAuth internals, static assets, and public auth pages
    "/((?!api/auth|_next/static|_next/image|favicon.ico|login|error).*)",
  ],
};
