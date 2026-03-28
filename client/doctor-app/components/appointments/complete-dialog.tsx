"use client";

// Confirmation dialog for marking an appointment as complete.
import { useRouter } from "next/navigation";
import { Button } from "@/components/ui/button";
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
  DialogTrigger,
} from "@/components/ui/dialog";
import { useCompleteAppointment } from "@/lib/hooks/use-doctor-appointments";

interface CompleteDialogProps {
  appointmentId: string;
}

export function CompleteDialog({ appointmentId }: CompleteDialogProps) {
  const router = useRouter();
  const { mutate, isPending } = useCompleteAppointment();

  function handleComplete() {
    mutate(appointmentId, {
      onSuccess: () => router.push("/appointments"),
    });
  }

  return (
    <Dialog>
      <DialogTrigger asChild>
        <Button size="sm">Mark Complete</Button>
      </DialogTrigger>
      <DialogContent>
        <DialogHeader>
          <DialogTitle>Complete Appointment</DialogTitle>
          <DialogDescription>Mark this appointment as completed?</DialogDescription>
        </DialogHeader>
        <DialogFooter>
          <Button onClick={handleComplete} disabled={isPending}>
            {isPending ? "Completing…" : "Confirm"}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}
