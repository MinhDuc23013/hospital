"use client";

// Schedule appointment page — react-hook-form + Zod + useScheduleAppointment mutation.
import { useRouter } from "next/navigation";
import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";
import { PageHeader } from "@/components/shared/page-header";
import { useProviders } from "@/lib/hooks/use-providers";
import { useScheduleAppointment } from "@/lib/hooks/use-appointments";
import { ScheduleAppointmentSchema, type ScheduleAppointmentInput } from "@/lib/validators";
import { toast } from "@/lib/hooks/use-toast";

export default function ScheduleAppointmentPage() {
  const router = useRouter();
  const { data: providers = [], isLoading: loadingProviders } = useProviders();
  const { mutate: schedule, isPending } = useScheduleAppointment();

  const {
    register,
    handleSubmit,
    setValue,
    formState: { errors },
  } = useForm<ScheduleAppointmentInput>({
    resolver: zodResolver(ScheduleAppointmentSchema),
    defaultValues: { duration: "PT30M" },
  });

  function onSubmit(data: ScheduleAppointmentInput) {
    // Convert local datetime-local value to ISO 8601
    const isoTime = new Date(data.scheduledTime).toISOString();
    schedule(
      { ...data, scheduledTime: isoTime },
      {
        onSuccess: () => {
          toast({ title: "Appointment scheduled successfully." });
          router.push("/appointments");
        },
        onError: () => {
          toast({ title: "Failed to schedule appointment.", variant: "destructive" });
        },
      }
    );
  }

  return (
    <div className="max-w-lg">
      <PageHeader title="Schedule Appointment" description="Book a new appointment with a provider." />
      <Card>
        <CardHeader>
          <CardTitle className="text-base">Appointment Details</CardTitle>
        </CardHeader>
        <CardContent>
          <form onSubmit={handleSubmit(onSubmit)} className="space-y-5">
            {/* Provider */}
            <div className="space-y-1.5">
              <Label htmlFor="providerId">Provider</Label>
              <Select
                disabled={loadingProviders}
                onValueChange={(val) => setValue("providerId", val, { shouldValidate: true })}
              >
                <SelectTrigger id="providerId">
                  <SelectValue placeholder={loadingProviders ? "Loading…" : "Select a provider"} />
                </SelectTrigger>
                <SelectContent>
                  {providers.map((p) => (
                    <SelectItem key={p.id} value={p.id}>
                      {p.name} — {p.specialty}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
              {errors.providerId && (
                <p className="text-xs text-destructive">{errors.providerId.message}</p>
              )}
            </div>

            {/* Date & Time */}
            <div className="space-y-1.5">
              <Label htmlFor="scheduledTime">Date & Time</Label>
              <Input
                id="scheduledTime"
                type="datetime-local"
                {...register("scheduledTime")}
              />
              {errors.scheduledTime && (
                <p className="text-xs text-destructive">{errors.scheduledTime.message}</p>
              )}
            </div>

            {/* Duration */}
            <div className="space-y-1.5">
              <Label htmlFor="duration">Duration</Label>
              <Select
                defaultValue="PT30M"
                onValueChange={(val) => setValue("duration", val)}
              >
                <SelectTrigger id="duration">
                  <SelectValue />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value="PT15M">15 minutes</SelectItem>
                  <SelectItem value="PT30M">30 minutes</SelectItem>
                  <SelectItem value="PT45M">45 minutes</SelectItem>
                  <SelectItem value="PT1H">1 hour</SelectItem>
                </SelectContent>
              </Select>
            </div>

            {/* Notes */}
            <div className="space-y-1.5">
              <Label htmlFor="notes">Notes (optional)</Label>
              <textarea
                id="notes"
                className="w-full rounded-md border px-3 py-2 text-sm resize-none focus:outline-none focus:ring-2 focus:ring-ring"
                rows={3}
                maxLength={500}
                placeholder="Describe your symptoms or concerns..."
                {...register("notes")}
              />
              {errors.notes && (
                <p className="text-xs text-destructive">{errors.notes.message}</p>
              )}
            </div>

            <div className="flex gap-3 pt-2">
              <Button type="button" variant="outline" onClick={() => router.back()}>
                Cancel
              </Button>
              <Button type="submit" disabled={isPending}>
                {isPending ? "Scheduling…" : "Schedule Appointment"}
              </Button>
            </div>
          </form>
        </CardContent>
      </Card>
    </div>
  );
}
