// Prescriptions list page — Server Component fetches prescriptions issued by this doctor.
import { getAuthSession } from "@/lib/auth-session";
import { callGatewayAPI } from "@/lib/api-client";
import { PageHeader } from "@/components/shared/page-header";
import { PrescriptionList } from "@/components/prescriptions/prescription-list";
import { Button } from "@/components/ui/button";
import Link from "next/link";
import type { Prescription } from "@/lib/types";

export default async function PrescriptionsPage() {
  const session = await getAuthSession();
  const doctorId = session?.user?.id ?? "";

  const prescriptions = await callGatewayAPI<Prescription[]>(
    `/api/prescriptions?providerId=${doctorId}`
  ).catch(() => [] as Prescription[]);

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <PageHeader title="Prescriptions" description="Prescriptions you have issued." />
        <Button asChild>
          <Link href="/prescriptions/new">New Prescription</Link>
        </Button>
      </div>
      <PrescriptionList prescriptions={prescriptions} />
    </div>
  );
}
