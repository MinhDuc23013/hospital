// Zod schemas for patient create/update operations.
import { z } from "zod";

export const createPatientSchema = z.object({
  email: z.string().email("Invalid email").max(255),
  firstName: z.string().min(1, "Required").max(100),
  lastName: z.string().min(1, "Required").max(100),
  dateOfBirth: z.string().min(1, "Required"),
  phoneNumber: z.string().max(20).optional().or(z.literal("")),
});

export const updatePatientSchema = z.object({
  firstName: z.string().min(1, "Required").max(100),
  lastName: z.string().min(1, "Required").max(100),
  phoneNumber: z.string().max(20).optional().or(z.literal("")),
});

export type CreatePatientInput = z.infer<typeof createPatientSchema>;
export type UpdatePatientInput = z.infer<typeof updatePatientSchema>;
