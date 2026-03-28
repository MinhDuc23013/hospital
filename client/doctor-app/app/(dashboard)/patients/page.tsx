// Patients list page — server component fetches assigned patients for current doctor.
import { getAuthSession } from "@/lib/auth-session";
import { callGatewayAPI } from "@/lib/api-client";
import { PageHeader } from "@/components/shared/page-header";
import { PatientList } from "@/components/patients/patient-list";
import type { Patient } from "@/lib/types";

export default async function PatientsPage() {
  const session = await getAuthSession();
  const doctorId = session?.user?.id ?? "";

  const patients = await callGatewayAPI<Patient[]>(
    `/api/patients?providerId=${doctorId}`
  ).catch(() => [] as Patient[]);

  return (
    <div className="space-y-6">
      <PageHeader title="Patients" description="Your assigned patients." />
      <PatientList initialPatients={patients} />
    </div>
  );
}
