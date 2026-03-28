// Patient profile — demographics card + quick-links + edit/deactivate actions.
import Link from "next/link";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { PatientDeleteDialog } from "@/components/patients/patient-delete-dialog";
import type { Patient } from "@/lib/types";

interface PatientProfileViewProps {
  patient: Patient;
}

export function PatientProfileView({ patient }: PatientProfileViewProps) {
  const dob = patient.dateOfBirth
    ? new Date(patient.dateOfBirth).toLocaleDateString()
    : "—";

  return (
    <div className="space-y-4">
      <Card>
        <CardHeader className="flex flex-row items-center justify-between">
          <CardTitle className="text-base">Demographics</CardTitle>
          <div className="flex items-center gap-2">
            <Badge variant={patient.isActive ? "default" : "secondary"}>
              {patient.isActive ? "Active" : "Inactive"}
            </Badge>
            {patient.isActive && (
              <>
                <Button variant="outline" size="sm" asChild>
                  <Link href={`/patients/${patient.id}/edit`}>Edit</Link>
                </Button>
                <PatientDeleteDialog
                  patientId={patient.id}
                  patientName={`${patient.firstName} ${patient.lastName}`}
                />
              </>
            )}
          </div>
        </CardHeader>
        <CardContent className="grid gap-2 text-sm sm:grid-cols-2">
          <div>
            <span className="font-medium">Name:</span> {patient.firstName}{" "}
            {patient.lastName}
          </div>
          <div>
            <span className="font-medium">Email:</span> {patient.email}
          </div>
          <div>
            <span className="font-medium">Date of Birth:</span> {dob}
          </div>
          <div>
            <span className="font-medium">Phone:</span>{" "}
            {patient.phoneNumber ?? "—"}
          </div>
        </CardContent>
      </Card>

      <div className="flex gap-3">
        <Button variant="outline" asChild>
          <Link href={`/appointments?patientId=${patient.id}`}>
            View Appointments
          </Link>
        </Button>
        <Button variant="outline" asChild>
          <Link href={`/medical-records?patientId=${patient.id}`}>
            View Medical Records
          </Link>
        </Button>
      </div>
    </div>
  );
}
