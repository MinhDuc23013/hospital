"use client";

// Form for creating a new medical record with dynamic lab results rows.
import { useRouter } from "next/navigation";
import { useForm, useFieldArray } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { createMedicalRecordSchema, type CreateMedicalRecordInput } from "@/lib/validators";
import { useCreateMedicalRecord } from "@/lib/hooks/use-medical-records";
import { Form, FormField, FormItem, FormLabel, FormControl, FormMessage } from "@/components/ui/form";
import { Input } from "@/components/ui/input";
import { Button } from "@/components/ui/button";
import { Card, CardContent } from "@/components/ui/card";
import { Plus, Trash2 } from "lucide-react";

export function CreateRecordForm() {
  const router = useRouter();
  const { mutate, isPending } = useCreateMedicalRecord();

  const form = useForm<CreateMedicalRecordInput>({
    resolver: zodResolver(createMedicalRecordSchema),
    defaultValues: { patientId: "", diagnosis: "", notes: "", labResults: [] },
  });

  const { fields, append, remove } = useFieldArray({
    control: form.control,
    name: "labResults",
  });

  function onSubmit(data: CreateMedicalRecordInput) {
    mutate(data, {
      onSuccess: (record) => router.push(`/medical-records/${record._id}`),
    });
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

            <FormField
              control={form.control}
              name="diagnosis"
              render={({ field }) => (
                <FormItem>
                  <FormLabel>Diagnosis</FormLabel>
                  <FormControl>
                    <textarea
                      className="flex min-h-[80px] w-full rounded-md border border-input bg-background px-3 py-2 text-sm placeholder:text-muted-foreground focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring disabled:opacity-50"
                      placeholder="Enter diagnosis..."
                      {...field}
                    />
                  </FormControl>
                  <FormMessage />
                </FormItem>
              )}
            />

            <FormField
              control={form.control}
              name="notes"
              render={({ field }) => (
                <FormItem>
                  <FormLabel>Notes (optional)</FormLabel>
                  <FormControl>
                    <textarea
                      className="flex min-h-[80px] w-full rounded-md border border-input bg-background px-3 py-2 text-sm placeholder:text-muted-foreground focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring disabled:opacity-50"
                      placeholder="Additional notes..."
                      {...field}
                    />
                  </FormControl>
                  <FormMessage />
                </FormItem>
              )}
            />

            <div className="space-y-2">
              <div className="flex items-center justify-between">
                <span className="text-sm font-medium">Lab Results</span>
                <Button type="button" variant="outline" size="sm" onClick={() => append({ name: "", value: "" })}>
                  <Plus className="mr-1 h-3.5 w-3.5" /> Add Row
                </Button>
              </div>
              {fields.map((field, index) => (
                <div key={field.id} className="flex gap-2 items-start">
                  <FormField
                    control={form.control}
                    name={`labResults.${index}.name`}
                    render={({ field }) => (
                      <FormItem className="flex-1">
                        <FormControl><Input placeholder="Test name" {...field} /></FormControl>
                        <FormMessage />
                      </FormItem>
                    )}
                  />
                  <FormField
                    control={form.control}
                    name={`labResults.${index}.value`}
                    render={({ field }) => (
                      <FormItem className="flex-1">
                        <FormControl><Input placeholder="Value" {...field} /></FormControl>
                        <FormMessage />
                      </FormItem>
                    )}
                  />
                  <Button type="button" variant="ghost" size="icon" onClick={() => remove(index)}>
                    <Trash2 className="h-4 w-4 text-destructive" />
                  </Button>
                </div>
              ))}
            </div>

            <Button type="submit" disabled={isPending} className="w-full">
              {isPending ? "Saving…" : "Create Record"}
            </Button>
          </form>
        </Form>
      </CardContent>
    </Card>
  );
}
