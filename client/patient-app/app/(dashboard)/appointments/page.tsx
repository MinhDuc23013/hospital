// Appointments list page — Server Component fetches initial data, passes to Client list.
import { getAuthSession } from "@/lib/auth-session";
import { callGatewayAPI } from "@/lib/api-client";
import { AppointmentList } from "@/components/appointments/appointment-list";
import { PageHeader } from "@/components/shared/page-header";
import type { Appointment } from "@/lib/types";

export default async function AppointmentsPage() {
  const session = await getAuthSession();
  const patientId = session?.user?.id ?? "";

  const appointments = await callGatewayAPI<Appointment[]>(
    `/api/appointments?patientId=${patientId}`
  ).catch(() => [] as Appointment[]);

  return (
    <div>
      <PageHeader title="Appointments" description="Manage your upcoming and past appointments." />
      <AppointmentList initialAppointments={appointments} />
    </div>
  );
}
