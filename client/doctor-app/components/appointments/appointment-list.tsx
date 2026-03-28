// List of AppointmentCard items with loading skeletons and empty state.
import { AppointmentCard } from "@/components/appointments/appointment-card";
import { EmptyState } from "@/components/shared/empty-state";
import type { Appointment } from "@/lib/types";

interface AppointmentListProps {
  appointments: Appointment[];
  isLoading?: boolean;
}

export function AppointmentList({ appointments, isLoading }: AppointmentListProps) {
  if (isLoading) {
    return (
      <div className="space-y-3">
        {[1, 2, 3].map((i) => (
          <div key={i} className="h-20 animate-pulse rounded-lg bg-muted" />
        ))}
      </div>
    );
  }

  if (appointments.length === 0) {
    return (
      <EmptyState
        title="No appointments found"
        description="No appointments match the selected filter."
      />
    );
  }

  return (
    <div className="space-y-3">
      {appointments.map((appt) => (
        <AppointmentCard key={appt.id} appointment={appt} />
      ))}
    </div>
  );
}
