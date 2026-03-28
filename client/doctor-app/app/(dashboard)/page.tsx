// Dashboard overview — fetches today's workload server-side
import { getAuthSession } from "@/lib/auth-session";
import { callGatewayAPI } from "@/lib/api-client";
import { PageHeader } from "@/components/shared/page-header";
import { StatsCards } from "@/components/dashboard/stats-cards";
import { TodaysAppointments } from "@/components/dashboard/todays-appointments";
import type { Appointment, Prescription } from "@/lib/types";

export default async function DashboardPage() {
  const session = await getAuthSession();
  const doctorId = session?.user?.id ?? "";
  const today = new Date().toISOString().split("T")[0];

  const [appointments, prescriptions] = await Promise.all([
    callGatewayAPI<Appointment[]>(
      `/api/appointments?providerId=${doctorId}&from=${today}&to=${today}&limit=20`
    ).catch(() => [] as Appointment[]),
    callGatewayAPI<Prescription[]>(
      `/api/prescriptions?providerId=${doctorId}&status=Pending`
    ).catch(() => [] as Prescription[]),
  ]);

  const completedCount = appointments.filter(a => a.status === "Completed").length;

  return (
    <div className="space-y-6">
      <PageHeader
        title="Dashboard"
        description="Your workload for today."
      />
      <StatsCards
        todayCount={appointments.length}
        completedCount={completedCount}
        pendingPrescriptions={prescriptions.length}
      />
      <TodaysAppointments appointments={appointments} />
    </div>
  );
}
