// Top navigation bar — displays user avatar, name, and logout button.
import { LogoutButton } from "@/components/auth/logout-button";
import type { Session } from "next-auth";

interface TopbarProps {
  user: Session["user"];
}

export function Topbar({ user }: TopbarProps) {
  const displayName = user?.name ?? user?.email ?? "Patient";
  const initial = displayName.charAt(0).toUpperCase();

  return (
    <header className="fixed top-0 right-0 left-0 z-20 flex h-16 items-center justify-between border-b bg-background px-4 md:left-64">
      {/* Left spacer — mobile toggle sits here on small screens */}
      <div className="w-10 md:hidden" />

      <div className="flex items-center gap-3 ml-auto">
        {/* Avatar */}
        <div className="flex h-8 w-8 items-center justify-center rounded-full bg-primary text-primary-foreground text-sm font-semibold select-none">
          {initial}
        </div>
        <span className="hidden sm:block text-sm font-medium">{displayName}</span>
        <LogoutButton size="sm" variant="ghost">
          Sign Out
        </LogoutButton>
      </div>
    </header>
  );
}
