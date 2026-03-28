// Card displaying a single appointment: provider, date/time, status, actions.
import Link from "next/link";
import { Card, CardContent } from "@/components/ui/card";
import { Button } from "@/components/ui/button";
import { StatusBadge } from "@/components/shared/status-badge";
import { formatDate, formatTime, formatDuration } from "@/lib/utils/date-utils";
import { CalendarDays, Clock } from "lucide-react";
import type { Appointment } from "@/lib/types";

interface AppointmentCardProps {
  appointment: Appointment;
  onCancel?: (appointment: Appointment) => void;
}

export function AppointmentCard({ appointment, onCancel }: AppointmentCardProps) {
  const canCancel = appointment.status === "Scheduled";

  return (
    <Card>
      <CardContent className="flex flex-col gap-3 p-4 sm:flex-row sm:items-center sm:justify-between">
        <div className="space-y-1">
          <div className="flex items-center gap-2">
            <StatusBadge status={appointment.status} />
          </div>
          <div className="flex items-center gap-2 text-sm text-muted-foreground">
            <CalendarDays className="h-4 w-4 shrink-0" />
            <span>{formatDate(appointment.scheduledTime)}</span>
          </div>
          <div className="flex items-center gap-2 text-sm text-muted-foreground">
            <Clock className="h-4 w-4 shrink-0" />
            <span>
              {formatTime(appointment.scheduledTime)} &middot; {formatDuration(appointment.duration)}
            </span>
          </div>
          {appointment.notes && (
            <p className="text-xs text-muted-foreground line-clamp-1">{appointment.notes}</p>
          )}
        </div>

        <div className="flex shrink-0 gap-2">
          <Button asChild variant="outline" size="sm">
            <Link href={`/appointments/${appointment.id}`}>View</Link>
          </Button>
          {canCancel && onCancel && (
            <Button
              variant="destructive"
              size="sm"
              onClick={() => onCancel(appointment)}
            >
              Cancel
            </Button>
          )}
        </div>
      </CardContent>
    </Card>
  );
}
