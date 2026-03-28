// Dashboard layout — wraps all protected pages with sidebar + topbar.
// Server Component: reads session server-side via getAuthSession().
import { redirect } from "next/navigation";
import { getAuthSession } from "@/lib/auth-session";
import { Sidebar } from "@/components/layout/sidebar";
import { Topbar } from "@/components/layout/topbar";

export default async function DashboardLayout({
  children,
}: {
  children: React.ReactNode;
}) {
  const session = await getAuthSession();
  if (!session) redirect("/login");
  // Force re-login if token refresh failed (e.g. Keycloak refresh token expired)
  if ((session as { error?: string }).error === "RefreshTokenError") redirect("/login");

  return (
    <div className="min-h-screen bg-background">
      <Sidebar />
      <Topbar user={session.user} />
      {/* Offset content for fixed sidebar (md+) and topbar */}
      <main className="pt-16 md:pl-64">
        <div className="p-6">{children}</div>
      </main>
    </div>
  );
}
