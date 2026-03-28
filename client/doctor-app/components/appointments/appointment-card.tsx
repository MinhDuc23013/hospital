// Card displaying a single appointment: patient, scheduled time, duration, status badge, link to detail.
import Link from "next/link";
import { Card, CardContent } from "@/components/ui/card";
import { Badge } from "@/components/ui/badge";
import type { Appointment } from "@/lib/types";

interface AppointmentCardProps {
  appointment: Appointment;
}

export function AppointmentCard({ appointment }: AppointmentCardProps) {
  return (
    <Link href={`/appointments/${appointment.id}`}>
      <Card className="hover:bg-muted/50 transition-colors cursor-pointer">
        <CardContent className="flex items-center justify-between p-4">
          <div className="space-y-1">
            <p className="text-sm font-medium">
              Patient {appointment.patientId.slice(0, 8)}&hellip;
            </p>
            <p className="text-xs text-muted-foreground">
              {new Date(appointment.scheduledTime).toLocaleString()} &middot; {appointment.duration}
            </p>
            {appointment.notes && (
              <p className="text-xs text-muted-foreground truncate max-w-xs">
                {appointment.notes}
              </p>
            )}
          </div>
          <Badge variant={appointment.status === "Completed" ? "secondary" : "default"}>
            {appointment.status}
          </Badge>
        </CardContent>
      </Card>
    </Link>
  );
}
