// Appointment detail page — Server Component fetches full appointment by ID.
import { notFound } from "next/navigation";
import Link from "next/link";
import { callGatewayAPI } from "@/lib/api-client";
import { PageHeader } from "@/components/shared/page-header";
import { StatusBadge } from "@/components/shared/status-badge";
import { Card, CardContent } from "@/components/ui/card";
import { Button } from "@/components/ui/button";
import { formatDate, formatTime, formatDuration } from "@/lib/utils/date-utils";
import type { Appointment } from "@/lib/types";

interface Props {
  params: { id: string };
}

export default async function AppointmentDetailPage({ params }: Props) {
  let appointment: Appointment;
  try {
    appointment = await callGatewayAPI<Appointment>(`/api/appointments/${params.id}`);
  } catch {
    notFound();
  }

  const canCancel = appointment.status === "Scheduled";

  return (
    <div className="max-w-lg space-y-6">
      <PageHeader
        title="Appointment Details"
        action={
          <Button asChild variant="outline" size="sm">
            <Link href="/appointments">Back to list</Link>
          </Button>
        }
      />

      <Card>
        <CardContent className="space-y-4 pt-6">
          <div className="flex items-center justify-between">
            <span className="text-sm text-muted-foreground">Status</span>
            <StatusBadge status={appointment.status} />
          </div>
          <div className="flex items-center justify-between">
            <span className="text-sm text-muted-foreground">Date</span>
            <span className="text-sm font-medium">{formatDate(appointment.scheduledTime)}</span>
          </div>
          <div className="flex items-center justify-between">
            <span className="text-sm text-muted-foreground">Time</span>
            <span className="text-sm font-medium">{formatTime(appointment.scheduledTime)}</span>
          </div>
          <div className="flex items-center justify-between">
            <span className="text-sm text-muted-foreground">Duration</span>
            <span className="text-sm font-medium">{formatDuration(appointment.duration)}</span>
          </div>
          {appointment.notes && (
            <div className="border-t pt-4">
              <p className="text-sm text-muted-foreground mb-1">Notes</p>
              <p className="text-sm">{appointment.notes}</p>
            </div>
          )}
        </CardContent>
      </Card>

      {canCancel && (
        <div className="flex justify-end">
          <Button asChild variant="destructive" size="sm">
            <Link href={`/appointments/${appointment.id}/cancel`}>Cancel Appointment</Link>
          </Button>
        </div>
      )}
    </div>
  );
}
