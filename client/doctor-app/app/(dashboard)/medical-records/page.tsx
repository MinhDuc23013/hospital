// Medical records list page — Server Component fetches records for this doctor.
import { getAuthSession } from "@/lib/auth-session";
import { callGatewayAPI } from "@/lib/api-client";
import { PageHeader } from "@/components/shared/page-header";
import { RecordList } from "@/components/medical-records/record-list";
import { Button } from "@/components/ui/button";
import Link from "next/link";
import type { MedicalRecord } from "@/lib/types";

interface MedicalRecordsPageProps {
  searchParams: { patientId?: string };
}

export default async function MedicalRecordsPage({ searchParams }: MedicalRecordsPageProps) {
  const session = await getAuthSession();
  const doctorId = session?.user?.id ?? "";
  const { patientId } = searchParams;

  const params = new URLSearchParams({ providerId: doctorId });
  if (patientId) params.set("patientId", patientId);

  const records = await callGatewayAPI<MedicalRecord[]>(
    `/api/medical-records?${params.toString()}`
  ).catch(() => [] as MedicalRecord[]);

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <PageHeader title="Medical Records" description="Patient records you have created." />
        <Button asChild>
          <Link href="/medical-records/new">New Record</Link>
        </Button>
      </div>
      <RecordList records={records} />
    </div>
  );
}
