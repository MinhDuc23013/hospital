// Zod schemas for patient profile form validation.
import { z } from "zod";

export const UpdatePatientSchema = z.object({
  firstName: z
    .string()
    .min(1, "First name is required")
    .max(100, "First name must be 100 characters or less"),
  lastName: z
    .string()
    .min(1, "Last name is required")
    .max(100, "Last name must be 100 characters or less"),
  phoneNumber: z
    .string()
    .regex(/^\+?[\d\s\-().]{7,20}$/, "Invalid phone number format")
    .optional()
    .or(z.literal("")),
  address: z
    .string()
    .max(255, "Address must be 255 characters or less")
    .optional(),
});

export type UpdatePatientInput = z.infer<typeof UpdatePatientSchema>;
