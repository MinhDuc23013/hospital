// Card for a single medical record: date, diagnosis preview, link to detail.
import Link from "next/link";
import { Card, CardContent } from "@/components/ui/card";
import { FileText } from "lucide-react";
import { formatDate } from "@/lib/utils/date-utils";
import type { MedicalRecord } from "@/lib/types";

interface RecordCardProps {
  record: MedicalRecord;
}

export function RecordCard({ record }: RecordCardProps) {
  const diagnosisPreview =
    record.diagnosis.length > 0
      ? record.diagnosis.slice(0, 2).join(", ") + (record.diagnosis.length > 2 ? "…" : "")
      : "No diagnosis listed";

  return (
    <Card className="transition-colors hover:bg-accent/50">
      <CardContent className="flex items-start gap-4 p-4">
        <div className="mt-0.5 shrink-0 rounded-md bg-muted p-2">
          <FileText className="h-5 w-5 text-muted-foreground" />
        </div>
        <div className="flex-1 min-w-0">
          <p className="text-sm font-medium truncate">{diagnosisPreview}</p>
          {record.findings && (
            <p className="mt-0.5 text-xs text-muted-foreground line-clamp-1">
              {record.findings}
            </p>
          )}
          <p className="mt-1 text-xs text-muted-foreground">{formatDate(record.createdAt)}</p>
        </div>
        <Link
          href={`/medical-records/${record._id}`}
          className="shrink-0 text-xs text-primary hover:underline"
        >
          View
        </Link>
      </CardContent>
    </Card>
  );
}
