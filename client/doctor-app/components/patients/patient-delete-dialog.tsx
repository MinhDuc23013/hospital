"use client";

// Confirmation dialog for deactivating a patient — shows name, warns irreversible.
import { useState } from "react";
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
import { useDeletePatient } from "@/lib/hooks/use-patients";
import { toast } from "@/lib/hooks/use-toast";

interface PatientDeleteDialogProps {
  patientId: string;
  patientName: string;
}

export function PatientDeleteDialog({ patientId, patientName }: PatientDeleteDialogProps) {
  const [open, setOpen] = useState(false);
  const router = useRouter();
  const { mutate: deletePatient, isPending } = useDeletePatient(patientId);

  function handleConfirm() {
    deletePatient(undefined, {
      onSuccess: () => {
        toast({ title: "Patient deactivated." });
        setOpen(false);
        router.push("/patients");
      },
      onError: () => {
        toast({ title: "Failed to deactivate patient.", variant: "destructive" });
      },
    });
  }

  return (
    <Dialog open={open} onOpenChange={setOpen}>
      <DialogTrigger asChild>
        <Button variant="destructive" size="sm">
          Deactivate
        </Button>
      </DialogTrigger>
      <DialogContent>
        <DialogHeader>
          <DialogTitle>Deactivate Patient</DialogTitle>
          <DialogDescription>
            Are you sure you want to deactivate <strong>{patientName}</strong>? This patient will
            no longer appear in active patient lists.
          </DialogDescription>
        </DialogHeader>
        <DialogFooter>
          <Button variant="outline" onClick={() => setOpen(false)} disabled={isPending}>
            Cancel
          </Button>
          <Button variant="destructive" onClick={handleConfirm} disabled={isPending}>
            {isPending ? "Deactivating…" : "Deactivate"}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}
