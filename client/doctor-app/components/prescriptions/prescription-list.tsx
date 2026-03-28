// List of prescription cards with empty state fallback.
import { Pill } from "lucide-react";
import { PrescriptionCard } from "./prescription-card";
import { EmptyState } from "@/components/shared/empty-state";
import type { Prescription } from "@/lib/types";

interface PrescriptionListProps {
  prescriptions: Prescription[];
}

export function PrescriptionList({ prescriptions }: PrescriptionListProps) {
  if (prescriptions.length === 0) {
    return (
      <EmptyState
        title="No prescriptions"
        description="Prescriptions you issue will appear here."
        icon={<Pill className="h-10 w-10" />}
      />
    );
  }

  return (
    <div className="space-y-3">
      {prescriptions.map((rx) => (
        <PrescriptionCard key={rx.id} prescription={rx} />
      ))}
    </div>
  );
}
