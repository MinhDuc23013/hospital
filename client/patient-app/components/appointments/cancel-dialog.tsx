"use client";

// Confirmation dialog for cancelling an appointment.
import { useState } from "react";
import { useRouter } from "next/navigation";
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";
import { Button } from "@/components/ui/button";
import { useCancelAppointment } from "@/lib/hooks/use-appointments";
import { toast } from "@/lib/hooks/use-toast";
import type { Appointment } from "@/lib/types";

interface CancelDialogProps {
  appointment: Appointment | null;
  open: boolean;
  onOpenChange: (open: boolean) => void;
}

export function CancelDialog({ appointment, open, onOpenChange }: CancelDialogProps) {
  const router = useRouter();
  const { mutate: cancelAppointment, isPending } = useCancelAppointment();
  const [reason, setReason] = useState("");

  function handleConfirm() {
    if (!appointment) return;
    cancelAppointment(
      { appointmentId: appointment.id, reason: reason || undefined },
      {
        onSuccess: () => {
          toast({ title: "Appointment cancelled successfully." });
          onOpenChange(false);
          router.refresh();
        },
        onError: () => {
          toast({ title: "Failed to cancel appointment.", variant: "destructive" });
        },
      }
    );
  }

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent>
        <DialogHeader>
          <DialogTitle>Cancel Appointment</DialogTitle>
          <DialogDescription>
            Are you sure you want to cancel this appointment? This action cannot be undone.
          </DialogDescription>
        </DialogHeader>
        <div className="py-2">
          <label className="text-sm font-medium" htmlFor="cancel-reason">
            Reason (optional)
          </label>
          <textarea
            id="cancel-reason"
            className="mt-1.5 w-full rounded-md border px-3 py-2 text-sm resize-none focus:outline-none focus:ring-2 focus:ring-ring"
            rows={3}
            maxLength={200}
            placeholder="Provide a reason..."
            value={reason}
            onChange={(e) => setReason(e.target.value)}
          />
        </div>
        <DialogFooter>
          <Button variant="outline" onClick={() => onOpenChange(false)} disabled={isPending}>
            Keep Appointment
          </Button>
          <Button variant="destructive" onClick={handleConfirm} disabled={isPending}>
            {isPending ? "Cancelling..." : "Yes, Cancel"}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}
