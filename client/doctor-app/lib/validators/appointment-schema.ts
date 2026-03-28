// Zod schemas for appointment form validation.
import { z } from "zod";

export const ScheduleAppointmentSchema = z.object({
  providerId: z.string().min(1, "Provider is required"),
  /** ISO 8601 datetime string, e.g. "2026-03-19T14:30:00Z" */
  scheduledTime: z.string().datetime({ message: "Valid date and time required" }),
  /** ISO 8601 duration, e.g. "PT30M" */
  duration: z.string().default("PT30M"),
  notes: z.string().max(500, "Notes must be 500 characters or less").optional(),
});

export const CancelAppointmentSchema = z.object({
  appointmentId: z.string().uuid("Invalid appointment ID"),
  reason: z.string().max(200, "Reason must be 200 characters or less").optional(),
});

export type ScheduleAppointmentInput = z.infer<typeof ScheduleAppointmentSchema>;
export type CancelAppointmentInput = z.infer<typeof CancelAppointmentSchema>;
