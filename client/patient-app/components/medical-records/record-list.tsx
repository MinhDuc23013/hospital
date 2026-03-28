"use client";

// Client Component — uses TanStack Query to support future filter refetch.
import { useSession } from "next-auth/react";
import { RecordCard } from "./record-card";
import { EmptyState } from "@/components/shared/empty-state";
import { useMedicalRecords } from "@/lib/hooks/use-medical-records";
import { FileText } from "lucide-react";
import type { MedicalRecord } from "@/lib/types";

interface RecordListProps {
  initialRecords: MedicalRecord[];
}

export function RecordList({ initialRecords }: RecordListProps) {
  const { data: session } = useSession();
  const patientId = session?.user?.id ?? "";

  // Only refetch via TanStack when patientId is available; use initial data otherwise
  const { data: records } = useMedicalRecords(patientId);
  const displayList = records ?? initialRecords;

  if (displayList.length === 0) {
    return (
      <EmptyState
        title="No medical records"
        description="Your medical records will appear here after your appointments."
        icon={<FileText className="h-10 w-10" />}
      />
    );
  }

  return (
    <div className="space-y-3">
      {displayList.map((record) => (
        <RecordCard key={record._id} record={record} />
      ))}
    </div>
  );
}
