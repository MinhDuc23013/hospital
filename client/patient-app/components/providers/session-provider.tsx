"use client";

// Thin wrapper so root layout.tsx (a Server Component) can import a named export
// without pulling in the 'use client' boundary at the layout level.
import { SessionProvider as NextAuthSessionProvider } from "next-auth/react";
import type { Session } from "next-auth";

interface SessionProviderProps {
  children: React.ReactNode;
  /** Pass initial session from Server Component to avoid waterfall on first render */
  session?: Session | null;
}

export function SessionProvider({ children, session }: SessionProviderProps) {
  return (
    <NextAuthSessionProvider session={session}>
      {children}
    </NextAuthSessionProvider>
  );
}
