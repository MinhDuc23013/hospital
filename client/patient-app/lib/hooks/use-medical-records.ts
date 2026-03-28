"use client";

// TanStack Query hooks for medical record data — all calls go through /api/proxy.
import { useQuery } from "@tanstack/react-query";
import type { MedicalRecord } from "@/lib/types";

export const MEDICAL_RECORDS_QUERY_KEY = "medical-records";

async function fetchMedicalRecords(patientId: string): Promise<MedicalRecord[]> {
  const res = await fetch(`/api/proxy/medical-records/${patientId}`);
  if (!res.ok) throw new Error(`Failed to fetch medical records: ${res.status}`);
  return res.json();
}

async function fetchMedicalRecord(id: string): Promise<MedicalRecord> {
  const res = await fetch(`/api/proxy/medical-records/${id}`);
  if (!res.ok) throw new Error(`Failed to fetch medical record: ${res.status}`);
  return res.json();
}

export function useMedicalRecords(patientId: string) {
  return useQuery({
    queryKey: [MEDICAL_RECORDS_QUERY_KEY, patientId],
    queryFn: () => fetchMedicalRecords(patientId),
    enabled: !!patientId,
  });
}

export function useMedicalRecord(id: string) {
  return useQuery({
    queryKey: [MEDICAL_RECORDS_QUERY_KEY, "detail", id],
    queryFn: () => fetchMedicalRecord(id),
    enabled: !!id,
  });
}
