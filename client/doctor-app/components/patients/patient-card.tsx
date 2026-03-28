// Card component for a single patient — links to patient profile page.
import Link from "next/link";
import { Card, CardContent } from "@/components/ui/card";
import { Badge } from "@/components/ui/badge";
import type { Patient } from "@/lib/types";

interface PatientCardProps {
  patient: Patient;
}

export function PatientCard({ patient }: PatientCardProps) {
  const dob = patient.dateOfBirth
    ? new Date(patient.dateOfBirth).toLocaleDateString()
    : "—";

  return (
    <Link href={`/patients/${patient.id}`}>
      <Card className="hover:bg-muted/50 transition-colors cursor-pointer">
        <CardContent className="flex items-center justify-between p-4">
          <div className="space-y-1">
            <p className="text-sm font-medium">
              {patient.firstName} {patient.lastName}
            </p>
            <p className="text-xs text-muted-foreground">{patient.email}</p>
            <p className="text-xs text-muted-foreground">
              DOB: {dob} · {patient.phoneNumber ?? "—"}
            </p>
          </div>
          <Badge variant={patient.isActive ? "default" : "secondary"}>
            {patient.isActive ? "Active" : "Inactive"}
          </Badge>
        </CardContent>
      </Card>
    </Link>
  );
}
