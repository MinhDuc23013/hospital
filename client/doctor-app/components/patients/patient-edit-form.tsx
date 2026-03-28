"use client";

// Form for editing patient demographics — react-hook-form + zod + useUpdatePatient mutation.
import { useRouter } from "next/navigation";
import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { useUpdatePatient } from "@/lib/hooks/use-patients";
import { updatePatientSchema, type UpdatePatientInput } from "@/lib/validators";
import { toast } from "@/lib/hooks/use-toast";
import type { Patient } from "@/lib/types";

interface PatientEditFormProps {
  patient: Patient;
}

export function PatientEditForm({ patient }: PatientEditFormProps) {
  const router = useRouter();
  const { mutate: updatePatient, isPending } = useUpdatePatient(patient.id);

  const {
    register,
    handleSubmit,
    formState: { errors },
  } = useForm<UpdatePatientInput>({
    resolver: zodResolver(updatePatientSchema),
    defaultValues: {
      firstName: patient.firstName,
      lastName: patient.lastName,
      phoneNumber: patient.phoneNumber ?? "",
    },
  });

  function onSubmit(data: UpdatePatientInput) {
    updatePatient(data, {
      onSuccess: () => {
        toast({ title: "Patient updated successfully." });
        router.push(`/patients/${patient.id}`);
      },
      onError: () => {
        toast({ title: "Failed to update patient.", variant: "destructive" });
      },
    });
  }

  return (
    <Card>
      <CardHeader>
        <CardTitle className="text-base">Edit Details</CardTitle>
      </CardHeader>
      <CardContent>
        <form onSubmit={handleSubmit(onSubmit)} className="space-y-5">
          <div className="grid gap-5 sm:grid-cols-2">
            <div className="space-y-1.5">
              <Label htmlFor="firstName">First Name</Label>
              <Input id="firstName" {...register("firstName")} />
              {errors.firstName && (
                <p className="text-xs text-destructive">{errors.firstName.message}</p>
              )}
            </div>

            <div className="space-y-1.5">
              <Label htmlFor="lastName">Last Name</Label>
              <Input id="lastName" {...register("lastName")} />
              {errors.lastName && (
                <p className="text-xs text-destructive">{errors.lastName.message}</p>
              )}
            </div>

            <div className="space-y-1.5">
              <Label htmlFor="phoneNumber">Phone Number</Label>
              <Input id="phoneNumber" type="tel" {...register("phoneNumber")} />
              {errors.phoneNumber && (
                <p className="text-xs text-destructive">{errors.phoneNumber.message}</p>
              )}
            </div>

            {/* Read-only fields */}
            <div className="space-y-1.5">
              <Label>Email</Label>
              <Input value={patient.email} readOnly disabled className="opacity-60" />
            </div>

            <div className="space-y-1.5">
              <Label>Date of Birth</Label>
              <Input
                value={patient.dateOfBirth ? new Date(patient.dateOfBirth).toLocaleDateString() : "—"}
                readOnly
                disabled
                className="opacity-60"
              />
            </div>
          </div>

          <div className="flex gap-3 pt-2">
            <Button type="button" variant="outline" onClick={() => router.back()}>
              Cancel
            </Button>
            <Button type="submit" disabled={isPending}>
              {isPending ? "Saving…" : "Save Changes"}
            </Button>
          </div>
        </form>
      </CardContent>
    </Card>
  );
}
