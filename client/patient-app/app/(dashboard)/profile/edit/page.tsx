"use client";

// Profile edit page — Client Component: loads patient via hook, renders edit form.
import { useSession } from "next-auth/react";
import { ProfileEditForm } from "@/components/profile/profile-edit-form";
import { PageHeader } from "@/components/shared/page-header";
import { usePatientProfile } from "@/lib/hooks/use-patient-profile";

export default function ProfileEditPage() {
  const { data: session } = useSession();
  const { data: patient, isLoading, isError } = usePatientProfile();

  if (!session || isLoading) {
    return (
      <div className="max-w-2xl space-y-4">
        <div className="h-8 w-48 animate-pulse rounded-md bg-muted" />
        <div className="h-64 animate-pulse rounded-lg bg-muted" />
      </div>
    );
  }

  if (isError || !patient) {
    return (
      <div className="py-16 text-center">
        <p className="text-sm text-muted-foreground">Failed to load profile. Please try again.</p>
      </div>
    );
  }

  return (
    <div className="max-w-2xl space-y-6">
      <PageHeader title="Edit Profile" description="Update your personal information." />
      <ProfileEditForm patient={patient} />
    </div>
  );
}
