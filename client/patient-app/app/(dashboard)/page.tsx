// Dashboard overview — Server Component that fetches initial data server-side.
import { getAuthSession } from "@/lib/auth-session";
import { callGatewayAPI } from "@/lib/api-client";
import { StatsCards } from "@/components/dashboard/stats-cards";
import { UpcomingAppointments } from "@/components/dashboard/upcoming-appointments";
import { RecentRecords } from "@/components/dashboard/recent-records";
import { PageHeader } from "@/components/shared/page-header";
import type { Appointment, MedicalRecord, Prescription } from "@/lib/types";

export default async function DashboardPage() {
  const session = await getAuthSession();
  const patientId = session?.user?.id ?? "";

  // Fetch all three data sources concurrently; return empty arrays on error
  const [appointments, records, prescriptions] = await Promise.all([
    callGatewayAPI<Appointment[]>(`/api/appointments?patientId=${patientId}&limit=5`).catch(() => [] as Appointment[]),
    callGatewayAPI<MedicalRecord[]>(`/api/medical-records/${patientId}?limit=3`).catch(() => [] as MedicalRecord[]),
    callGatewayAPI<Prescription[]>(`/api/prescriptions?patientId=${patientId}`).catch(() => [] as Prescription[]),
  ]);

  const upcomingAppointments = appointments.filter(
    (a) => a.status === "Scheduled" || a.status === "InProgress"
  );
  const activePrescriptions = prescriptions.filter((p) => p.status === "Pending").length;

  return (
    <div className="space-y-6">
      <PageHeader title="Dashboard" description="Welcome back. Here's your health summary." />
      <StatsCards
        upcomingCount={upcomingAppointments.length}
        totalRecords={records.length}
        activePrescriptions={activePrescriptions}
      />
      <div className="grid gap-6 md:grid-cols-2">
        <UpcomingAppointments appointments={upcomingAppointments.slice(0, 5)} />
        <RecentRecords records={records.slice(0, 3)} />
      </div>
    </div>
  );
}
