// New prescription creation page.
import { PageHeader } from "@/components/shared/page-header";
import { CreatePrescriptionForm } from "@/components/prescriptions/create-prescription-form";

export default function NewPrescriptionPage() {
  return (
    <div className="space-y-6">
      <PageHeader title="New Prescription" description="Issue a prescription for a patient." />
      <CreatePrescriptionForm />
    </div>
  );
}
