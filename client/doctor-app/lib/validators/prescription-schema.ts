// Zod schema for creating prescriptions — doctor-only operation.
import { z } from "zod";

export const createPrescriptionSchema = z.object({
  patientId: z.string().uuid("Invalid patient ID"),
  appointmentId: z.string().uuid().optional(),
  drugId: z.string().min(1, "Drug required"),
  dosage: z.string().min(1, "Dosage required"),
  instructions: z.string().max(500).optional(),
  durationDays: z.number().int().positive("Duration must be positive"),
});

export type CreatePrescriptionInput = z.infer<typeof createPrescriptionSchema>;
