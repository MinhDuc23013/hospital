// Edit patient page — server component fetches patient, renders edit form.
import { notFound } from "next/navigation";
import { callGatewayAPI } from "@/lib/api-client";
import { PageHeader } from "@/components/shared/page-header";
import { PatientEditForm } from "@/components/patients/patient-edit-form";
import type { Patient } from "@/lib/types";

export default async function EditPatientPage({
  params,
}: {
  params: { id: string };
}) {
  const patient = await callGatewayAPI<Patient>(
    `/api/patients/${params.id}`
  ).catch(() => null);

  if (!patient) notFound();

  return (
    <div className="max-w-2xl space-y-6">
      <PageHeader
        title={`Edit ${patient.firstName} ${patient.lastName}`}
        description="Update patient information."
      />
      <PatientEditForm patient={patient} />
    </div>
  );
}
