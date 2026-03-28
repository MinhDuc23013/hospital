// Auth error page — displayed by NextAuth on authentication failures
// NextAuth redirects here with ?error=<ErrorCode> query param
import Link from "next/link";
import { Card, CardContent, CardDescription, CardFooter, CardHeader, CardTitle } from "@/components/ui/card";
import { Button } from "@/components/ui/button";

interface AuthErrorPageProps {
  searchParams: { error?: string };
}

const ERROR_MESSAGES: Record<string, { title: string; description: string }> = {
  Configuration: {
    title: "Server Configuration Error",
    description: "There is a problem with the server configuration. Please contact support.",
  },
  AccessDenied: {
    title: "Access Denied",
    description: "You do not have permission to sign in. Please contact your care team.",
  },
  Verification: {
    title: "Verification Failed",
    description: "The sign-in link is no longer valid. It may have expired or already been used.",
  },
  Default: {
    title: "Authentication Error",
    description: "An unexpected error occurred during sign-in. Please try again.",
  },
};

export default function AuthErrorPage({ searchParams }: AuthErrorPageProps) {
  const errorCode = searchParams.error ?? "Default";
  const { title, description } =
    ERROR_MESSAGES[errorCode] ?? ERROR_MESSAGES.Default;

  return (
    <Card className="shadow-md">
      <CardHeader className="space-y-1 text-center">
        <div className="mx-auto mb-2 flex h-12 w-12 items-center justify-center rounded-full bg-destructive/10">
          <svg
            className="h-6 w-6 text-destructive"
            fill="none"
            stroke="currentColor"
            viewBox="0 0 24 24"
            aria-hidden="true"
          >
            <path
              strokeLinecap="round"
              strokeLinejoin="round"
              strokeWidth={2}
              d="M12 9v2m0 4h.01m-6.938 4h13.856c1.54 0 2.502-1.667 1.732-2.5L13.732 4c-.77-.834-1.964-.834-2.732 0L3.07 16.5c-.77.833.192 2.5 1.732 2.5z"
            />
          </svg>
        </div>
        <CardTitle className="text-xl font-bold">{title}</CardTitle>
        <CardDescription>{description}</CardDescription>
      </CardHeader>

      <CardContent>
        {errorCode !== "Default" && (
          <p className="text-center text-xs text-muted-foreground">
            Error code: <code className="font-mono">{errorCode}</code>
          </p>
        )}
      </CardContent>

      <CardFooter className="flex justify-center">
        <Button asChild variant="default">
          <Link href="/login">Back to Sign In</Link>
        </Button>
      </CardFooter>
    </Card>
  );
}
