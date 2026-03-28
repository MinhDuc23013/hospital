"use client";

// Hooks for patient data — queries and mutations via /api/proxy.
import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import type { Patient } from "@/lib/types";
import type { CreatePatientInput, UpdatePatientInput } from "@/lib/validators";

export const PATIENTS_KEY = "patients";

export function usePatients(providerId?: string) {
  return useQuery({
    queryKey: [PATIENTS_KEY, providerId],
    queryFn: async (): Promise<Patient[]> => {
      const query = providerId ? `?providerId=${providerId}` : "";
      const res = await fetch(`/api/proxy/patients${query}`);
      if (!res.ok) throw new Error(`Failed to fetch patients: ${res.status}`);
      return res.json();
    },
    enabled: !!providerId,
  });
}

export function useCreatePatient() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async (data: CreatePatientInput): Promise<Patient> => {
      const res = await fetch("/api/proxy/patients", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify(data),
      });
      if (!res.ok) throw new Error(`Failed to create patient: ${res.status}`);
      return res.json();
    },
    onSuccess: () => queryClient.invalidateQueries({ queryKey: [PATIENTS_KEY] }),
  });
}

export function useUpdatePatient(id: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async (data: UpdatePatientInput): Promise<Patient> => {
      const res = await fetch(`/api/proxy/patients/${id}`, {
        method: "PUT",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ id, ...data }),
      });
      if (!res.ok) throw new Error(`Failed to update patient: ${res.status}`);
      return res.json();
    },
    onSuccess: () => queryClient.invalidateQueries({ queryKey: [PATIENTS_KEY] }),
  });
}

export function useDeletePatient(id: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async () => {
      const res = await fetch(`/api/proxy/patients/${id}`, { method: "DELETE" });
      if (!res.ok) throw new Error(`Failed to deactivate patient: ${res.status}`);
    },
    onSuccess: () => queryClient.invalidateQueries({ queryKey: [PATIENTS_KEY] }),
  });
}
