"use client";

// Login page — Keycloak OIDC sign-in via NextAuth v5
// Security: no callbackUrl from query params (prevents open redirect attacks).
// NextAuth v5 handles the post-login redirect internally from session state.
import { signIn } from "next-auth/react";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import { Button } from "@/components/ui/button";

interface LoginPageProps {
  searchParams: { error?: string };
}

const AUTH_ERRORS: Record<string, string> = {
  OAuthSignin: "Error starting sign-in. Please try again.",
  OAuthCallback: "Error during sign-in callback. Please try again.",
  OAuthCreateAccount: "Could not create account. Please contact support.",
  Callback: "Sign-in callback error. Please try again.",
  OAuthAccountNotLinked: "Account not linked. Please use the same sign-in method.",
  SessionRequired: "Please sign in to access this page.",
  Default: "An authentication error occurred. Please try again.",
};

function getErrorMessage(error?: string): string | null {
  if (!error) return null;
  return AUTH_ERRORS[error] ?? AUTH_ERRORS.Default;
}

export default function LoginPage({ searchParams }: LoginPageProps) {
  const errorMessage = getErrorMessage(searchParams.error);

  const handleSignIn = () => {
    signIn("keycloak", { callbackUrl: "/" });
  };

  return (
    <Card className="shadow-md">
      <CardHeader className="space-y-1 text-center">
        <div className="mx-auto mb-2 flex h-12 w-12 items-center justify-center rounded-full bg-primary/10">
          {/* Hospital logo placeholder */}
          <svg
            className="h-6 w-6 text-primary"
            fill="none"
            stroke="currentColor"
            viewBox="0 0 24 24"
            aria-hidden="true"
          >
            <path
              strokeLinecap="round"
              strokeLinejoin="round"
              strokeWidth={2}
              d="M19 21V5a2 2 0 00-2-2H7a2 2 0 00-2 2v16m14 0h2m-2 0h-5m-9 0H3m2 0h5M9 7h1m-1 4h1m4-4h1m-1 4h1m-5 10v-5a1 1 0 011-1h2a1 1 0 011 1v5m-4 0h4"
            />
          </svg>
        </div>
        <CardTitle className="text-2xl font-bold">Doctor Portal</CardTitle>
        <CardDescription>
          Sign in to manage your patients and appointments
        </CardDescription>
      </CardHeader>

      <CardContent className="space-y-4">
        {errorMessage && (
          <div
            role="alert"
            className="rounded-md border border-destructive/50 bg-destructive/10 px-4 py-3 text-sm text-destructive"
          >
            {errorMessage}
          </div>
        )}

        <Button
          onClick={handleSignIn}
          className="w-full"
          size="lg"
        >
          Sign In with Hospital Account
        </Button>

        <p className="text-center text-xs text-muted-foreground">
          Your account is managed by the hospital administration.
          <br />
          Contact your care team if you need access.
        </p>
      </CardContent>
    </Card>
  );
}
