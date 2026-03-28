"use client";

// Client Component — appointment fields + Complete / Cancel action dialogs.
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Badge } from "@/components/ui/badge";
import { CompleteDialog } from "@/components/appointments/complete-dialog";
import { CancelDialog } from "@/components/appointments/cancel-dialog";
import type { Appointment } from "@/lib/types";

interface AppointmentDetailProps {
  appointment: Appointment;
}

export function AppointmentDetail({ appointment }: AppointmentDetailProps) {
  const canAct = appointment.status === "Scheduled" || appointment.status === "InProgress";

  return (
    <Card>
      <CardHeader className="flex flex-row items-center justify-between">
        <CardTitle className="text-base">Appointment Details</CardTitle>
        <Badge>{appointment.status}</Badge>
      </CardHeader>
      <CardContent className="space-y-4">
        <div className="grid gap-2 text-sm">
          <div><span className="font-medium">Patient ID:</span> {appointment.patientId}</div>
          <div>
            <span className="font-medium">Scheduled:</span>{" "}
            {new Date(appointment.scheduledTime).toLocaleString()}
          </div>
          <div><span className="font-medium">Duration:</span> {appointment.duration}</div>
          {appointment.notes && (
            <div><span className="font-medium">Notes:</span> {appointment.notes}</div>
          )}
        </div>
        {canAct && (
          <div className="flex gap-2 pt-2">
            <CompleteDialog appointmentId={appointment.id} />
            <CancelDialog appointmentId={appointment.id} />
          </div>
        )}
      </CardContent>
    </Card>
  );
}
