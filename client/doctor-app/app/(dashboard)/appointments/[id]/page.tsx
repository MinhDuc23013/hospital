// Appointment detail page — Server Component fetches by ID.
import { notFound } from "next/navigation";
import { callGatewayAPI } from "@/lib/api-client";
import { PageHeader } from "@/components/shared/page-header";
import { AppointmentDetail } from "@/components/appointments/appointment-detail";
import type { Appointment } from "@/lib/types";

interface Props {
  params: { id: string };
}

export default async function AppointmentDetailPage({ params }: Props) {
  const appointment = await callGatewayAPI<Appointment>(`/api/appointments/${params.id}`)
    .catch(() => null);

  if (!appointment) notFound();

  return (
    <div className="space-y-6">
      <PageHeader title="Appointment Detail" />
      <AppointmentDetail appointment={appointment} />
    </div>
  );
}
