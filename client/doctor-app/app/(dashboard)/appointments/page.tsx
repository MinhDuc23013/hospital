"use client";

// Appointments list page with status filter tabs.
import { useState } from "react";
import { PageHeader } from "@/components/shared/page-header";
import { Tabs, TabsList, TabsTrigger, TabsContent } from "@/components/ui/tabs";
import { AppointmentList } from "@/components/appointments/appointment-list";
import { useDoctorAppointments } from "@/lib/hooks/use-doctor-appointments";

const STATUSES = ["All", "Scheduled", "Completed", "Cancelled"] as const;

export default function AppointmentsPage() {
  const [status, setStatus] = useState<string>("All");
  const { data: appointments = [], isLoading } = useDoctorAppointments(
    status !== "All" ? { status } : undefined
  );

  return (
    <div className="space-y-6">
      <PageHeader title="Appointments" description="All your appointments." />
      <Tabs value={status} onValueChange={setStatus}>
        <TabsList>
          {STATUSES.map((s) => (
            <TabsTrigger key={s} value={s}>{s}</TabsTrigger>
          ))}
        </TabsList>
        {STATUSES.map((s) => (
          <TabsContent key={s} value={s}>
            <AppointmentList appointments={appointments} isLoading={isLoading} />
          </TabsContent>
        ))}
      </Tabs>
    </div>
  );
}
