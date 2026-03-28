"use client";

// Client Component — handles status filter tabs and TanStack Query refetch on filter change.
import { useState } from "react";
import Link from "next/link";
import { Button } from "@/components/ui/button";
import { Tabs, TabsList, TabsTrigger } from "@/components/ui/tabs";
import { AppointmentCard } from "./appointment-card";
import { CancelDialog } from "./cancel-dialog";
import { EmptyState } from "@/components/shared/empty-state";
import { useAppointments } from "@/lib/hooks/use-appointments";
import type { Appointment, AppointmentStatus } from "@/lib/types";
import { CalendarDays } from "lucide-react";

type FilterTab = "All" | AppointmentStatus;

const TABS: FilterTab[] = ["All", "Scheduled", "Completed", "Cancelled"];

interface AppointmentListProps {
  initialAppointments: Appointment[];
}

export function AppointmentList({ initialAppointments }: AppointmentListProps) {
  const [activeTab, setActiveTab] = useState<FilterTab>("All");
  const [cancelTarget, setCancelTarget] = useState<Appointment | null>(null);

  const filters = activeTab !== "All" ? { status: activeTab } : undefined;
  const { data: appointments, isLoading } = useAppointments(filters);

  // Use server-fetched data initially; switch to TanStack data on filter change
  const displayList = activeTab === "All" && !appointments ? initialAppointments : (appointments ?? []);

  return (
    <div className="space-y-4">
      <div className="flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
        <Tabs value={activeTab} onValueChange={(v) => setActiveTab(v as FilterTab)}>
          <TabsList>
            {TABS.map((tab) => (
              <TabsTrigger key={tab} value={tab}>
                {tab}
              </TabsTrigger>
            ))}
          </TabsList>
        </Tabs>
        <Button asChild size="sm">
          <Link href="/appointments/schedule">+ Schedule New</Link>
        </Button>
      </div>

      {isLoading ? (
        <div className="space-y-3">
          {[1, 2, 3].map((i) => (
            <div key={i} className="h-24 animate-pulse rounded-lg bg-muted" />
          ))}
        </div>
      ) : displayList.length === 0 ? (
        <EmptyState
          title="No appointments found"
          description={activeTab === "All" ? "Schedule your first appointment." : `No ${activeTab.toLowerCase()} appointments.`}
          icon={<CalendarDays className="h-10 w-10" />}
          action={
            <Button asChild size="sm">
              <Link href="/appointments/schedule">Schedule Appointment</Link>
            </Button>
          }
        />
      ) : (
        <div className="space-y-3">
          {displayList.map((appt) => (
            <AppointmentCard
              key={appt.id}
              appointment={appt}
              onCancel={setCancelTarget}
            />
          ))}
        </div>
      )}

      <CancelDialog
        appointment={cancelTarget}
        open={!!cancelTarget}
        onOpenChange={(open) => { if (!open) setCancelTarget(null); }}
      />
    </div>
  );
}
