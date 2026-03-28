// Card for a single prescription: patient ID, drug, dosage, status badge, date.
import { Card, CardContent } from "@/components/ui/card";
import { StatusBadge } from "@/components/shared/status-badge";
import { formatDate } from "@/lib/utils/date-utils";
import { Pill } from "lucide-react";
import type { Prescription } from "@/lib/types";

interface PrescriptionCardProps {
  prescription: Prescription;
}

export function PrescriptionCard({ prescription }: PrescriptionCardProps) {
  const patientShort = prescription.patientId.length > 16
    ? prescription.patientId.slice(0, 16) + "…"
    : prescription.patientId;

  return (
    <Card>
      <CardContent className="flex items-start gap-4 p-4">
        <div className="mt-0.5 shrink-0 rounded-md bg-muted p-2">
          <Pill className="h-5 w-5 text-muted-foreground" />
        </div>
        <div className="flex-1 space-y-1 min-w-0">
          <div className="flex items-center justify-between gap-2">
            <p className="text-sm font-medium truncate">Drug #{prescription.drugId}</p>
            <StatusBadge status={prescription.status} />
          </div>
          <p className="text-xs text-muted-foreground">Patient: {patientShort}</p>
          <p className="text-xs text-muted-foreground">
            {prescription.instructions && <span>{prescription.instructions} &middot; </span>}
            Issued: {formatDate(prescription.issuedAt)}
          </p>
        </div>
      </CardContent>
    </Card>
  );
}
