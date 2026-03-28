// Zod schema for creating medical records — doctor-only operation.
import { z } from "zod";

export const createMedicalRecordSchema = z.object({
  patientId: z.string().uuid("Invalid patient ID"),
  diagnosis: z.string().min(1, "Diagnosis required").max(500),
  notes: z.string().max(2000).optional(),
  labResults: z.array(z.object({
    name: z.string().min(1),
    value: z.string().min(1),
  })).optional(),
});

export type CreateMedicalRecordInput = z.infer<typeof createMedicalRecordSchema>;
