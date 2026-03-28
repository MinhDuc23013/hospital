"use client";

// Form for issuing a new prescription.
import { useRouter } from "next/navigation";
import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { createPrescriptionSchema, type CreatePrescriptionInput } from "@/lib/validators";
import { useCreatePrescription } from "@/lib/hooks/use-prescriptions";
import { Form, FormField, FormItem, FormLabel, FormControl, FormMessage } from "@/components/ui/form";
import { Input } from "@/components/ui/input";
import { Button } from "@/components/ui/button";
import { Card, CardContent } from "@/components/ui/card";

export function CreatePrescriptionForm() {
  const router = useRouter();
  const { mutate, isPending } = useCreatePrescription();

  const form = useForm<CreatePrescriptionInput>({
    resolver: zodResolver(createPrescriptionSchema),
    defaultValues: {
      patientId: "",
      drugId: "",
      dosage: "",
      durationDays: 1,
      instructions: "",
      appointmentId: "",
    },
  });

  function onSubmit(data: CreatePrescriptionInput) {
    // Strip empty optional strings before submitting
    const payload: CreatePrescriptionInput = {
      ...data,
      instructions: data.instructions || undefined,
      appointmentId: data.appointmentId || undefined,
    };
    mutate(payload, { onSuccess: () => router.push("/prescriptions") });
  }

  return (
    <Card>
      <CardContent className="pt-6">
        <Form {...form}>
          <form onSubmit={form.handleSubmit(onSubmit)} className="space-y-4">
            <FormField
              control={form.control}
              name="patientId"
              render={({ field }) => (
                <FormItem>
                  <FormLabel>Patient ID (UUID)</FormLabel>
                  <FormControl><Input placeholder="xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx" {...field} /></FormControl>
                  <FormMessage />
                </FormItem>
              )}
            />

            <div className="grid grid-cols-2 gap-4">
              <FormField
                control={form.control}
                name="drugId"
                render={({ field }) => (
                  <FormItem>
                    <FormLabel>Drug ID</FormLabel>
                    <FormControl><Input placeholder="e.g. MED-001" {...field} /></FormControl>
                    <FormMessage />
                  </FormItem>
                )}
              />
              <FormField
                control={form.control}
                name="dosage"
                render={({ field }) => (
                  <FormItem>
                    <FormLabel>Dosage</FormLabel>
                    <FormControl><Input placeholder="e.g. 500mg twice daily" {...field} /></FormControl>
                    <FormMessage />
                  </FormItem>
                )}
              />
            </div>

            <FormField
              control={form.control}
              name="durationDays"
              render={({ field }) => (
                <FormItem>
                  <FormLabel>Duration (days)</FormLabel>
                  <FormControl>
                    <Input
                      type="number"
                      min={1}
                      {...field}
                      onChange={(e) => field.onChange(Number(e.target.value))}
                    />
                  </FormControl>
                  <FormMessage />
                </FormItem>
              )}
            />

            <FormField
              control={form.control}
              name="instructions"
              render={({ field }) => (
                <FormItem>
                  <FormLabel>Instructions (optional)</FormLabel>
                  <FormControl>
                    <textarea
                      className="flex min-h-[72px] w-full rounded-md border border-input bg-background px-3 py-2 text-sm placeholder:text-muted-foreground focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring disabled:opacity-50"
                      placeholder="Take with food..."
                      {...field}
                    />
                  </FormControl>
                  <FormMessage />
                </FormItem>
              )}
            />

            <FormField
              control={form.control}
              name="appointmentId"
              render={({ field }) => (
                <FormItem>
                  <FormLabel>Appointment ID (optional)</FormLabel>
                  <FormControl><Input placeholder="xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx" {...field} /></FormControl>
                  <FormMessage />
                </FormItem>
              )}
            />

            <Button type="submit" disabled={isPending} className="w-full">
              {isPending ? "Issuing…" : "Issue Prescription"}
            </Button>
          </form>
        </Form>
      </CardContent>
    </Card>
  );
}
