// Dashboard widget: next 5 upcoming appointments with date + status badge.
import Link from "next/link";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Button } from "@/components/ui/button";
import { StatusBadge } from "@/components/shared/status-badge";
import { EmptyState } from "@/components/shared/empty-state";
import { formatDate, formatTime } from "@/lib/utils/date-utils";
import type { Appointment } from "@/lib/types";

interface UpcomingAppointmentsProps {
  appointments: Appointment[];
}

export function UpcomingAppointments({ appointments }: UpcomingAppointmentsProps) {
  return (
    <Card>
      <CardHeader className="flex flex-row items-center justify-between pb-2">
        <CardTitle className="text-base font-semibold">Upcoming Appointments</CardTitle>
        <Button asChild variant="ghost" size="sm">
          <Link href="/appointments">View all</Link>
        </Button>
      </CardHeader>
      <CardContent>
        {appointments.length === 0 ? (
          <EmptyState
            title="No upcoming appointments"
            description="Schedule one to get started."
            className="border-none py-6"
          />
        ) : (
          <ul className="divide-y">
            {appointments.map((appt) => (
              <li key={appt.id} className="flex items-center justify-between py-3">
                <div>
                  <p className="text-sm font-medium">{formatDate(appt.scheduledTime)}</p>
                  <p className="text-xs text-muted-foreground">{formatTime(appt.scheduledTime)}</p>
                </div>
                <StatusBadge status={appt.status} />
              </li>
            ))}
          </ul>
        )}
      </CardContent>
    </Card>
  );
}
