"use client";

// Confirmation dialog for cancelling an appointment with optional reason field.
import { useState } from "react";
import { useRouter } from "next/navigation";
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
  DialogTrigger,
} from "@/components/ui/dialog";
import { Button } from "@/components/ui/button";
import { useCancelAppointment } from "@/lib/hooks/use-doctor-appointments";
import { toast } from "@/lib/hooks/use-toast";

interface CancelDialogProps {
  appointmentId: string;
}

export function CancelDialog({ appointmentId }: CancelDialogProps) {
  const router = useRouter();
  const { mutate: cancelAppointment, isPending } = useCancelAppointment();
  const [open, setOpen] = useState(false);
  const [reason, setReason] = useState("");

  function handleConfirm() {
    cancelAppointment(
      { appointmentId, reason: reason || "" },
      {
        onSuccess: () => {
          toast({ title: "Appointment cancelled successfully." });
          setOpen(false);
          router.push("/appointments");
        },
        onError: () => {
          toast({ title: "Failed to cancel appointment.", variant: "destructive" });
        },
      }
    );
  }

  return (
    <Dialog open={open} onOpenChange={setOpen}>
      <DialogTrigger asChild>
        <Button size="sm" variant="destructive">Cancel Appointment</Button>
      </DialogTrigger>
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
          <Button variant="outline" onClick={() => setOpen(false)} disabled={isPending}>
            Keep Appointment
          </Button>
          <Button variant="destructive" onClick={handleConfirm} disabled={isPending}>
            {isPending ? "Cancelling…" : "Yes, Cancel"}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}
