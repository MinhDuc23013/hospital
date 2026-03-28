// GET /api/auth/token — returns idToken for Keycloak SSO end_session logout.
// The idToken is stored server-side only (never in client session).
// logout-button.tsx fetches this to build the Keycloak end_session URL.
// Only exposes idToken — accessToken is never returned.
import { NextResponse } from "next/server";
import { auth } from "@/lib/auth-config";

export async function GET() {
  const session = await auth();

  if (!session) {
    return NextResponse.json({ idToken: null }, { status: 200 });
  }

  // idToken lives in the server-side JWT, not in the session object exposed to clients.
  const token = session as unknown as { idToken?: string };

  // no-store: idToken must never be cached by browser or CDN
  return NextResponse.json(
    { idToken: token.idToken ?? null },
    { headers: { "Cache-Control": "no-store" } }
  );
}
