// Profile view — displays patient info as read-only labeled fields.
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { formatDate } from "@/lib/utils/date-utils";
import { formatName } from "@/lib/utils/format-utils";
import type { Patient } from "@/lib/types";

interface ProfileViewProps {
  patient: Patient;
}

function Field({ label, value }: { label: string; value?: string }) {
  return (
    <div className="flex flex-col gap-0.5">
      <span className="text-xs font-medium text-muted-foreground uppercase tracking-wide">
        {label}
      </span>
      <span className="text-sm">{value || "—"}</span>
    </div>
  );
}

export function ProfileView({ patient }: ProfileViewProps) {
  return (
    <Card>
      <CardHeader>
        <CardTitle className="text-base">Personal Information</CardTitle>
      </CardHeader>
      <CardContent className="grid gap-5 sm:grid-cols-2">
        <Field label="Full Name" value={formatName(patient.firstName, patient.lastName)} />
        <Field label="Email" value={patient.email} />
        <Field label="Phone" value={patient.phoneNumber} />
        <Field label="Date of Birth" value={patient.dateOfBirth ? formatDate(patient.dateOfBirth) : undefined} />
        <Field label="Address" value={patient.address} />
        <Field label="Member Since" value={formatDate(patient.createdAt)} />
      </CardContent>
    </Card>
  );
}
