// Prescriptions list page — Server Component fetches initial list, passes to Client component.
import { getAuthSession } from "@/lib/auth-session";
import { callGatewayAPI } from "@/lib/api-client";
import { PrescriptionList } from "@/components/prescriptions/prescription-list";
import { PageHeader } from "@/components/shared/page-header";
import type { Prescription } from "@/lib/types";

export default async function PrescriptionsPage() {
  const session = await getAuthSession();
  const patientId = session?.user?.id ?? "";

  const prescriptions = await callGatewayAPI<Prescription[]>(
    `/api/prescriptions?patientId=${patientId}`
  ).catch(() => [] as Prescription[]);

  return (
    <div>
      <PageHeader
        title="Prescriptions"
        description="View your current and past prescriptions."
      />
      <PrescriptionList initialPrescriptions={prescriptions} />
    </div>
  );
}
