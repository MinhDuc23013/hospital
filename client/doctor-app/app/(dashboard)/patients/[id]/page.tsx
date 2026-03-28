// Patient profile page — server component fetches single patient by ID.
import { notFound } from "next/navigation";
import { callGatewayAPI } from "@/lib/api-client";
import { PageHeader } from "@/components/shared/page-header";
import { PatientProfileView } from "@/components/patients/patient-profile-view";
import type { Patient } from "@/lib/types";

export default async function PatientProfilePage({
  params,
}: {
  params: { id: string };
}) {
  const patient = await callGatewayAPI<Patient>(
    `/api/patients/${params.id}`
  ).catch(() => null);

  if (!patient) notFound();

  return (
    <div className="space-y-6">
      <PageHeader
        title={`${patient.firstName} ${patient.lastName}`}
        description="Patient profile"
      />
      <PatientProfileView patient={patient} />
    </div>
  );
}
