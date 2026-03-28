// Medical records list page — Server Component fetches initial records, passes to Client list.
import { getAuthSession } from "@/lib/auth-session";
import { callGatewayAPI } from "@/lib/api-client";
import { RecordList } from "@/components/medical-records/record-list";
import { PageHeader } from "@/components/shared/page-header";
import type { MedicalRecord } from "@/lib/types";

export default async function MedicalRecordsPage() {
  const session = await getAuthSession();
  const patientId = session?.user?.id ?? "";

  const records = await callGatewayAPI<MedicalRecord[]>(
    `/api/medical-records/${patientId}`
  ).catch(() => [] as MedicalRecord[]);

  return (
    <div>
      <PageHeader
        title="Medical Records"
        description="View your complete medical history."
      />
      <RecordList initialRecords={records} />
    </div>
  );
}
