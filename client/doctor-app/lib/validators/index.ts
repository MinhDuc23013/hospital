// Barrel export for all Zod validation schemas — import from "@/lib/validators"
export {
  ScheduleAppointmentSchema,
  CancelAppointmentSchema,
} from "./appointment-schema";
export type {
  ScheduleAppointmentInput,
  CancelAppointmentInput,
} from "./appointment-schema";

export { createMedicalRecordSchema } from "./medical-record-schema";
export type { CreateMedicalRecordInput } from "./medical-record-schema";

export { createPrescriptionSchema } from "./prescription-schema";
export type { CreatePrescriptionInput } from "./prescription-schema";

export { createPatientSchema, updatePatientSchema } from "./patient-validators";
export type { CreatePatientInput, UpdatePatientInput } from "./patient-validators";
