"use client";

// Profile edit form — react-hook-form + UpdatePatientSchema + useUpdateProfile mutation.
import { useRouter } from "next/navigation";
import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { useUpdateProfile } from "@/lib/hooks/use-patient-profile";
import { UpdatePatientSchema, type UpdatePatientInput } from "@/lib/validators";
import { toast } from "@/lib/hooks/use-toast";
import { formatDate } from "@/lib/utils/date-utils";
import type { Patient } from "@/lib/types";

interface ProfileEditFormProps {
  patient: Patient;
}

export function ProfileEditForm({ patient }: ProfileEditFormProps) {
  const router = useRouter();
  const { mutate: updateProfile, isPending } = useUpdateProfile();

  const {
    register,
    handleSubmit,
    formState: { errors },
  } = useForm<UpdatePatientInput>({
    resolver: zodResolver(UpdatePatientSchema),
    defaultValues: {
      firstName: patient.firstName,
      lastName: patient.lastName,
      phoneNumber: patient.phoneNumber ?? "",
      address: patient.address ?? "",
    },
  });

  function onSubmit(data: UpdatePatientInput) {
    updateProfile(data, {
      onSuccess: () => {
        toast({ title: "Profile updated successfully." });
        router.push("/profile");
      },
      onError: () => {
        toast({ title: "Failed to update profile.", variant: "destructive" });
      },
    });
  }

  return (
    <Card>
      <CardHeader>
        <CardTitle className="text-base">Edit Profile</CardTitle>
      </CardHeader>
      <CardContent>
        <form onSubmit={handleSubmit(onSubmit)} className="space-y-5">
          <div className="grid gap-5 sm:grid-cols-2">
            {/* First Name */}
            <div className="space-y-1.5">
              <Label htmlFor="firstName">First Name</Label>
              <Input id="firstName" {...register("firstName")} />
              {errors.firstName && (
                <p className="text-xs text-destructive">{errors.firstName.message}</p>
              )}
            </div>

            {/* Last Name */}
            <div className="space-y-1.5">
              <Label htmlFor="lastName">Last Name</Label>
              <Input id="lastName" {...register("lastName")} />
              {errors.lastName && (
                <p className="text-xs text-destructive">{errors.lastName.message}</p>
              )}
            </div>

            {/* Phone */}
            <div className="space-y-1.5">
              <Label htmlFor="phoneNumber">Phone Number</Label>
              <Input id="phoneNumber" type="tel" {...register("phoneNumber")} />
              {errors.phoneNumber && (
                <p className="text-xs text-destructive">{errors.phoneNumber.message}</p>
              )}
            </div>

            {/* Email — read-only */}
            <div className="space-y-1.5">
              <Label htmlFor="email">Email</Label>
              <Input id="email" value={patient.email} readOnly disabled className="opacity-60" />
            </div>

            {/* DOB — read-only */}
            <div className="space-y-1.5">
              <Label htmlFor="dob">Date of Birth</Label>
              <Input
                id="dob"
                value={patient.dateOfBirth ? formatDate(patient.dateOfBirth) : "—"}
                readOnly
                disabled
                className="opacity-60"
              />
            </div>
          </div>

          {/* Address — full width */}
          <div className="space-y-1.5">
            <Label htmlFor="address">Address</Label>
            <Input id="address" {...register("address")} />
            {errors.address && (
              <p className="text-xs text-destructive">{errors.address.message}</p>
            )}
          </div>

          <div className="flex gap-3 pt-2">
            <Button type="button" variant="outline" onClick={() => router.push("/profile")}>
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
