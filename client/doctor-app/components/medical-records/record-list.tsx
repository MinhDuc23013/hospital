// List of medical record cards with empty state fallback.
import { FileText } from "lucide-react";
import { RecordCard } from "./record-card";
import { EmptyState } from "@/components/shared/empty-state";
import type { MedicalRecord } from "@/lib/types";

interface RecordListProps {
  records: MedicalRecord[];
}

export function RecordList({ records }: RecordListProps) {
  if (records.length === 0) {
    return (
      <EmptyState
        title="No medical records"
        description="Medical records created for patients will appear here."
        icon={<FileText className="h-10 w-10" />}
      />
    );
  }

  return (
    <div className="space-y-3">
      {records.map((record) => (
        <RecordCard key={record._id} record={record} />
      ))}
    </div>
  );
}
