// Medical record detail page — fetches by ID and renders read-only view.
import { notFound } from "next/navigation";
import { callGatewayAPI } from "@/lib/api-client";
import { PageHeader } from "@/components/shared/page-header";
import { RecordDetail } from "@/components/medical-records/record-detail";
import type { MedicalRecord } from "@/lib/types";

export default async function MedicalRecordDetailPage({ params }: { params: { id: string } }) {
  const record = await callGatewayAPI<MedicalRecord>(`/api/medical-records/${params.id}`)
    .catch(() => null);

  if (!record) notFound();

  return (
    <div className="space-y-6">
      <PageHeader title="Medical Record" />
      <RecordDetail record={record} />
    </div>
  );
}
