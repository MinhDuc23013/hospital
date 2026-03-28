// Profile view page — Server Component fetches patient data server-side.
import { notFound } from "next/navigation";
import Link from "next/link";
import { getAuthSession } from "@/lib/auth-session";
import { callGatewayAPI } from "@/lib/api-client";
import { ProfileView } from "@/components/profile/profile-view";
import { PageHeader } from "@/components/shared/page-header";
import { Button } from "@/components/ui/button";
import type { Patient } from "@/lib/types";

export default async function ProfilePage() {
  const session = await getAuthSession();
  const patientId = session?.user?.id ?? "";

  let patient: Patient;
  try {
    patient = await callGatewayAPI<Patient>(`/api/patients/${patientId}`);
  } catch {
    notFound();
  }

  return (
    <div className="max-w-2xl space-y-6">
      <PageHeader
        title="My Profile"
        description="View and manage your personal information."
        action={
          <Button asChild size="sm">
            <Link href="/profile/edit">Edit Profile</Link>
          </Button>
        }
      />
      <ProfileView patient={patient} />
    </div>
  );
}
