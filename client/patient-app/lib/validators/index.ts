// Barrel export for all Zod validation schemas — import from "@/lib/validators"
export {
  ScheduleAppointmentSchema,
  CancelAppointmentSchema,
} from "./appointment-schema";
export type {
  ScheduleAppointmentInput,
  CancelAppointmentInput,
} from "./appointment-schema";

export { UpdatePatientSchema } from "./patient-schema";
export type { UpdatePatientInput } from "./patient-schema";
