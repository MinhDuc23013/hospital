// New medical record creation page.
import { PageHeader } from "@/components/shared/page-header";
import { CreateRecordForm } from "@/components/medical-records/create-record-form";

export default function NewMedicalRecordPage() {
  return (
    <div className="space-y-6">
      <PageHeader title="New Medical Record" description="Create a medical record for a patient." />
      <CreateRecordForm />
    </div>
  );
}
