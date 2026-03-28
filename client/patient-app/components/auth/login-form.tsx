"use client";

// Reusable sign-in trigger button — can be embedded in any page.
// Uses the same no-callbackUrl pattern as the login page (open redirect prevention).
import { signIn } from "next-auth/react";
import { Button } from "@/components/ui/button";
import type { ButtonProps } from "@/components/ui/button";

interface LoginFormProps extends Omit<ButtonProps, "onClick"> {
  children?: React.ReactNode;
}

export function LoginForm({
  children = "Sign In with Hospital Account",
  className,
  ...props
}: LoginFormProps) {
  const handleSignIn = () => {
    // No callbackUrl — NextAuth v5 handles post-login redirect from session state
    signIn("keycloak");
  };

  return (
    <Button onClick={handleSignIn} className={className} {...props}>
      {children}
    </Button>
  );
}
