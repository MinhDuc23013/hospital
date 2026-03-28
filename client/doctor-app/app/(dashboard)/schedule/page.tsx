// Schedule page — Server Component fetching this week's appointments for the logged-in doctor.
import { getAuthSession } from "@/lib/auth-session";
import { callGatewayAPI } from "@/lib/api-client";
import { PageHeader } from "@/components/shared/page-header";
import { AppointmentList } from "@/components/appointments/appointment-list";
import type { Appointment } from "@/lib/types";

function getWeekBounds() {
  const now = new Date();
  const day = now.getDay();
  const monday = new Date(now);
  monday.setDate(now.getDate() - (day === 0 ? 6 : day - 1));
  const sunday = new Date(monday);
  sunday.setDate(monday.getDate() + 6);
  return {
    from: monday.toISOString().split("T")[0],
    to: sunday.toISOString().split("T")[0],
  };
}

export default async function SchedulePage() {
  const session = await getAuthSession();
  const doctorId = session?.user?.id ?? "";
  const { from, to } = getWeekBounds();

  const appointments = await callGatewayAPI<Appointment[]>(
    `/api/appointments?providerId=${doctorId}&from=${from}&to=${to}`
  ).catch(() => [] as Appointment[]);

  const sorted = [...appointments].sort(
    (a, b) => new Date(a.scheduledTime).getTime() - new Date(b.scheduledTime).getTime()
  );

  return (
    <div className="space-y-6">
      <PageHeader title="My Schedule" description={`Week of ${from} – ${to}`} />
      <AppointmentList appointments={sorted} />
    </div>
  );
}
