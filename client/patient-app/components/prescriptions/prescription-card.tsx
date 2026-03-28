// Card for a single prescription: drug ID, dosage, valid until, status badge.
import { Card, CardContent } from "@/components/ui/card";
import { StatusBadge } from "@/components/shared/status-badge";
import { formatDate } from "@/lib/utils/date-utils";
import { Pill } from "lucide-react";
import type { Prescription } from "@/lib/types";

interface PrescriptionCardProps {
  prescription: Prescription;
}

export function PrescriptionCard({ prescription }: PrescriptionCardProps) {
  return (
    <Card>
      <CardContent className="flex items-start gap-4 p-4">
        <div className="mt-0.5 shrink-0 rounded-md bg-muted p-2">
          <Pill className="h-5 w-5 text-muted-foreground" />
        </div>
        <div className="flex-1 space-y-1">
          <div className="flex items-center justify-between gap-2">
            <p className="text-sm font-medium">Drug #{prescription.drugId}</p>
            <StatusBadge status={prescription.status} />
          </div>
          <p className="text-xs text-muted-foreground">
            Qty: {prescription.quantity} &middot; {prescription.instructions}
          </p>
          <p className="text-xs text-muted-foreground">
            Issued: {formatDate(prescription.issuedAt)} &middot; Valid until: {formatDate(prescription.validUntil)}
          </p>
        </div>
      </CardContent>
    </Card>
  );
}
