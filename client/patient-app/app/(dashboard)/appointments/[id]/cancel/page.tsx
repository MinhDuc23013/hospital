"use client";

// Cancel appointment confirmation page — standalone page that wraps the CancelDialog flow.
import { useParams, useRouter } from "next/navigation";
import { useEffect, useState } from "react";
import { CancelDialog } from "@/components/appointments/cancel-dialog";
import { useAppointment } from "@/lib/hooks/use-appointments";

export default function CancelAppointmentPage() {
  const { id } = useParams<{ id: string }>();
  const router = useRouter();
  const { data: appointment, isLoading } = useAppointment(id);
  const [open, setOpen] = useState(true);

  // When dialog closes (cancelled or confirmed), go back to the detail page
  function handleOpenChange(isOpen: boolean) {
    setOpen(isOpen);
    if (!isOpen) router.push(`/appointments/${id}`);
  }

  // Once data is loaded, open the dialog (it's already open by default)
  useEffect(() => {
    if (!isLoading && !appointment) {
      router.push("/appointments");
    }
  }, [isLoading, appointment, router]);

  if (isLoading) {
    return (
      <div className="flex items-center justify-center py-16">
        <p className="text-sm text-muted-foreground">Loading…</p>
      </div>
    );
  }

  return (
    <CancelDialog
      appointment={appointment ?? null}
      open={open}
      onOpenChange={handleOpenChange}
    />
  );
}
