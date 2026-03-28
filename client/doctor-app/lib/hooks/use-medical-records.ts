"use client";

// Hook for fetching and creating medical records via proxy API.
import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import type { MedicalRecord } from "@/lib/types";
import type { CreateMedicalRecordInput } from "@/lib/validators";

export const RECORDS_KEY = "medical-records";

export function useMedicalRecords(filters?: { patientId?: string }) {
  return useQuery({
    queryKey: [RECORDS_KEY, filters],
    queryFn: async (): Promise<MedicalRecord[]> => {
      const params = new URLSearchParams();
      if (filters?.patientId) params.set("patientId", filters.patientId);
      const query = params.toString() ? `?${params.toString()}` : "";
      const res = await fetch(`/api/proxy/medical-records${query}`);
      if (!res.ok) throw new Error(`Failed to fetch records: ${res.status}`);
      return res.json();
    },
  });
}

export function useCreateMedicalRecord() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async (data: CreateMedicalRecordInput): Promise<MedicalRecord> => {
      const res = await fetch("/api/proxy/medical-records", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify(data),
      });
      if (!res.ok) throw new Error(`Failed to create record: ${res.status}`);
      return res.json();
    },
    onSuccess: () => queryClient.invalidateQueries({ queryKey: [RECORDS_KEY] }),
  });
}
