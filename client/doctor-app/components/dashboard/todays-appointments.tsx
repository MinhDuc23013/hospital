import Link from "next/link";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Badge } from "@/components/ui/badge";
import { EmptyState } from "@/components/shared/empty-state";
import type { Appointment } from "@/lib/types";

interface TodaysAppointmentsProps {
  appointments: Appointment[];
}

export function TodaysAppointments({ appointments }: TodaysAppointmentsProps) {
  const sorted = [...appointments].sort(
    (a, b) => new Date(a.scheduledTime).getTime() - new Date(b.scheduledTime).getTime()
  );

  return (
    <Card>
      <CardHeader>
        <CardTitle className="text-base">Today&apos;s Schedule</CardTitle>
      </CardHeader>
      <CardContent>
        {sorted.length === 0 ? (
          <EmptyState title="No appointments today" description="Your schedule is clear." />
        ) : (
          <ul className="space-y-3">
            {sorted.map((appt) => (
              <li key={appt.id}>
                <Link
                  href={`/appointments/${appt.id}`}
                  className="flex items-center justify-between rounded-md border p-3 hover:bg-muted/50 transition-colors"
                >
                  <div className="space-y-0.5">
                    <p className="text-sm font-medium">Patient {appt.patientId.slice(0, 8)}&hellip;</p>
                    <p className="text-xs text-muted-foreground">
                      {new Date(appt.scheduledTime).toLocaleTimeString([], { hour: "2-digit", minute: "2-digit" })}
                      {" · "}
                      {appt.duration}
                    </p>
                  </div>
                  <Badge variant={appt.status === "Completed" ? "secondary" : "default"}>
                    {appt.status}
                  </Badge>
                </Link>
              </li>
            ))}
          </ul>
        )}
      </CardContent>
    </Card>
  );
}
