import { describe, it, expect, vi } from "vitest";

// Mock next/server and auth-session so vitest doesn't attempt to load
// the full Next.js / next-auth runtime (incompatible with jsdom).
import { isAllowedPath } from "@/app/api/proxy/[...path]/allowed-paths";

describe("isAllowedPath", () => {
  // allowed paths
  it("allows appointments list", () => expect(isAllowedPath("appointments")).toBe(true));
  it("allows appointments/:id", () => expect(isAllowedPath("appointments/abc-123")).toBe(true));
  it("allows appointments/:id/complete", () => expect(isAllowedPath("appointments/abc-123/complete")).toBe(true));
  it("allows appointments/:id/cancel", () => expect(isAllowedPath("appointments/abc-123/cancel")).toBe(true));
  it("allows patients list", () => expect(isAllowedPath("patients")).toBe(true));
  it("allows patients/:id", () => expect(isAllowedPath("patients/abc-123")).toBe(true));
  it("allows medical-records list", () => expect(isAllowedPath("medical-records")).toBe(true));
  it("allows medical-records/:id", () => expect(isAllowedPath("medical-records/abc-123")).toBe(true));
  it("allows prescriptions list", () => expect(isAllowedPath("prescriptions")).toBe(true));
  it("allows prescriptions/:id", () => expect(isAllowedPath("prescriptions/abc-123")).toBe(true));
  it("allows providers", () => expect(isAllowedPath("providers")).toBe(true));
  // blocked paths
  it("blocks admin path", () => expect(isAllowedPath("admin/users")).toBe(false));
  it("blocks unknown path", () => expect(isAllowedPath("something-else")).toBe(false));
  it("blocks nested admin", () => expect(isAllowedPath("appointments/abc/notes/private")).toBe(false));
});
