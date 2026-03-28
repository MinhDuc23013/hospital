"use client";

// Hook for fetching and creating prescriptions via proxy API.
import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import type { Prescription } from "@/lib/types";
import type { CreatePrescriptionInput } from "@/lib/validators";

export const PRESCRIPTIONS_KEY = "prescriptions";

export function usePrescriptions(filters?: { patientId?: string; status?: string }) {
  return useQuery({
    queryKey: [PRESCRIPTIONS_KEY, filters],
    queryFn: async (): Promise<Prescription[]> => {
      const params = new URLSearchParams();
      if (filters?.patientId) params.set("patientId", filters.patientId);
      if (filters?.status) params.set("status", filters.status);
      const query = params.toString() ? `?${params.toString()}` : "";
      const res = await fetch(`/api/proxy/prescriptions${query}`);
      if (!res.ok) throw new Error(`Failed to fetch prescriptions: ${res.status}`);
      return res.json();
    },
  });
}

export function useCreatePrescription() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async (data: CreatePrescriptionInput): Promise<Prescription> => {
      const res = await fetch("/api/proxy/prescriptions", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify(data),
      });
      if (!res.ok) throw new Error(`Failed to create prescription: ${res.status}`);
      return res.json();
    },
    onSuccess: () => queryClient.invalidateQueries({ queryKey: [PRESCRIPTIONS_KEY] }),
  });
}
