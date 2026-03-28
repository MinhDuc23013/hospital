import { describe, it, expect } from "vitest";
import { createMedicalRecordSchema } from "@/lib/validators/medical-record-schema";
import { createPrescriptionSchema } from "@/lib/validators/prescription-schema";

describe("createMedicalRecordSchema", () => {
  it("accepts valid input", () => {
    const result = createMedicalRecordSchema.safeParse({
      patientId: "550e8400-e29b-41d4-a716-446655440000",
      diagnosis: "Hypertension",
    });
    expect(result.success).toBe(true);
  });

  it("rejects missing diagnosis", () => {
    const result = createMedicalRecordSchema.safeParse({
      patientId: "550e8400-e29b-41d4-a716-446655440000",
    });
    expect(result.success).toBe(false);
  });

  it("rejects invalid UUID", () => {
    const result = createMedicalRecordSchema.safeParse({
      patientId: "not-a-uuid",
      diagnosis: "Test",
    });
    expect(result.success).toBe(false);
  });
});

describe("createPrescriptionSchema", () => {
  it("accepts valid input", () => {
    const result = createPrescriptionSchema.safeParse({
      patientId: "550e8400-e29b-41d4-a716-446655440000",
      drugId: "AMOX-500",
      dosage: "500mg twice daily",
      durationDays: 7,
    });
    expect(result.success).toBe(true);
  });

  it("rejects zero duration", () => {
    const result = createPrescriptionSchema.safeParse({
      patientId: "550e8400-e29b-41d4-a716-446655440000",
      drugId: "AMOX-500",
      dosage: "500mg",
      durationDays: 0,
    });
    expect(result.success).toBe(false);
  });

  it("rejects missing drugId", () => {
    const result = createPrescriptionSchema.safeParse({
      patientId: "550e8400-e29b-41d4-a716-446655440000",
      dosage: "500mg",
      durationDays: 7,
    });
    expect(result.success).toBe(false);
  });
});
