// New patient page — wraps PatientCreateForm client component.
import { PageHeader } from "@/components/shared/page-header";
import { PatientCreateForm } from "@/components/patients/patient-create-form";

export default function NewPatientPage() {
  return (
    <div className="max-w-2xl space-y-6">
      <PageHeader title="New Patient" description="Register a new patient in the system." />
      <PatientCreateForm />
    </div>
  );
}
